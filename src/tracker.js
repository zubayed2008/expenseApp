'use strict';

const fs = require('node:fs');
const path = require('node:path');

const DEFAULT_FILE = 'expenses.md';
const DEFAULT_CURRENCY = 'USD';

const CATEGORIES = {
  expense: [
    'Food',
    'Groceries',
    'Rent',
    'Utilities',
    'Transport',
    'Health',
    'Shopping',
    'Entertainment',
    'Travel',
    'Education',
    'Subscriptions',
    'Other',
  ],
  income: [
    'Salary',
    'Bonus',
    'Freelance',
    'Business',
    'Investments',
    'Gifts',
    'Refunds',
    'Other',
  ],
};

const PAYMENT_METHODS = [
  'Card',
  'Cash',
  'Bank transfer',
  'Mobile wallet',
  'Direct debit',
  'Other',
];

const TYPE_LABELS = {
  expense: 'Expense',
  income: 'Income',
};

/* ------------------------------------------------------------------ *
 * Money helpers
 * ------------------------------------------------------------------ */

/**
 * Parse user input such as "12", "12.5", "1,234.56" or "$9.99" into cents.
 * Throws when the value is not a positive amount with at most 2 decimals.
 */
function parseAmountToCents(value) {
  if (typeof value === 'number') {
    if (!Number.isFinite(value)) {
      throw new Error(`Invalid amount: ${value}.`);
    }
    const cents = Math.round(value * 100);
    if (cents <= 0) {
      throw new Error('Amount must be greater than zero.');
    }
    return cents;
  }

  const text = String(value ?? '').trim().replace(/[\s,$£€]/g, '');
  if (!/^\d+(\.\d{1,2})?$/.test(text)) {
    throw new Error(`Invalid amount: "${value}". Use a positive number like 12.50.`);
  }

  const [whole, fraction = ''] = text.split('.');
  const cents = Number(whole) * 100 + Number(fraction.padEnd(2, '0'));
  if (!Number.isSafeInteger(cents) || cents <= 0) {
    throw new Error('Amount must be greater than zero.');
  }
  return cents;
}

/** Validate an integer number of cents (used by the interactive CLI). */
function validateCents(value) {
  const cents = Number(value);
  if (!Number.isSafeInteger(cents) || cents <= 0) {
    throw new Error('Amount must be greater than zero.');
  }
  return cents;
}

function normalizeCurrency(value) {
  const code = String(value ?? '').trim().toUpperCase();
  if (!/^[A-Z]{3}$/.test(code)) {
    throw new Error(
      `Invalid currency code: "${value}". Use a 3-letter code like USD or GBP.`,
    );
  }
  return code;
}

/** Format an integer amount of cents using the document currency. */
function formatMoney(cents, currency = DEFAULT_CURRENCY) {
  let code = DEFAULT_CURRENCY;
  try {
    code = normalizeCurrency(currency);
  } catch {
    // Fall back to the default currency for display purposes.
  }
  try {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: code,
    }).format(cents / 100);
  } catch {
    return `${code} ${(cents / 100).toFixed(2)}`;
  }
}

/** Plain two-decimal amount, safe inside markdown tables. */
function formatAmountPlain(cents) {
  return (cents / 100).toFixed(2);
}

/* ------------------------------------------------------------------ *
 * Date helpers
 * ------------------------------------------------------------------ */

function pad(value) {
  return String(value).padStart(2, '0');
}

function todayISO(now = new Date()) {
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`;
}

function currentMonth(now = new Date()) {
  return todayISO(now).slice(0, 7);
}

/** Accepts YYYY-MM-DD, "today" and "yesterday"; empty means today. */
function parseDate(value) {
  const text = String(value ?? '').trim().toLowerCase();
  const now = new Date();

  if (!text || text === 'today') {
    return todayISO(now);
  }
  if (text === 'yesterday') {
    const yesterday = new Date(now);
    yesterday.setDate(yesterday.getDate() - 1);
    return todayISO(yesterday);
  }

  if (!/^\d{4}-\d{2}-\d{2}$/.test(text)) {
    throw new Error(
      `Invalid date: "${value}". Use YYYY-MM-DD, today, or yesterday.`,
    );
  }

  const [year, month, day] = text.split('-').map(Number);
  const parsed = new Date(Date.UTC(year, month - 1, day));
  if (
    parsed.getUTCFullYear() !== year ||
    parsed.getUTCMonth() !== month - 1 ||
    parsed.getUTCDate() !== day
  ) {
    throw new Error(`Invalid date: "${value}". That day does not exist.`);
  }
  return text;
}

function normalizeMonth(value) {
  const text = String(value ?? '').trim();
  if (!/^\d{4}-(0[1-9]|1[0-2])$/.test(text)) {
    throw new Error(`Invalid month: "${value}". Use YYYY-MM, for example 2026-10.`);
  }
  return text;
}

function formatTimestamp(date) {
  return `${todayISO(date)} ${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

/* ------------------------------------------------------------------ *
 * Transactions
 * ------------------------------------------------------------------ */

/** Validate and normalise a transaction coming from flags or prompts. */
function normalizeTransaction(input = {}) {
  const type = String(input.type ?? '').trim().toLowerCase();
  if (type !== 'expense' && type !== 'income') {
    throw new Error(
      `Unknown transaction type: "${input.type}". Use "expense" or "income".`,
    );
  }

  const amount =
    input.amountCents !== undefined
      ? validateCents(input.amountCents)
      : parseAmountToCents(input.amount);

  return {
    type,
    date: parseDate(input.date),
    category: String(input.category ?? '').trim() || 'Other',
    description: String(input.description ?? '')
      .replace(/\s+/g, ' ')
      .trim(),
    method:
      String(input.method ?? '').trim() ||
      (type === 'income' ? 'Bank transfer' : 'Card'),
    amount,
  };
}

function compareTransactions(a, b) {
  return (
    a.date.localeCompare(b.date) ||
    a.type.localeCompare(b.type) ||
    a.id.localeCompare(b.id)
  );
}

function nextId(transactions, type) {
  const prefix = type === 'income' ? 'inc' : 'exp';
  let max = 0;
  for (const transaction of transactions) {
    if (transaction.type !== type) continue;
    const numeric = Number(String(transaction.id).replace(/[^0-9]/g, ''));
    if (Number.isFinite(numeric)) max = Math.max(max, numeric);
  }
  return `${prefix}-${String(max + 1).padStart(4, '0')}`;
}

/** Fill in IDs for hand-added rows and de-duplicate colliding IDs. */
function assignMissingIds(transactions) {
  const counters = { expense: 0, income: 0 };
  for (const transaction of transactions) {
    const numeric = Number(String(transaction.id).replace(/[^0-9]/g, ''));
    if (Number.isFinite(numeric) && numeric > 0) {
      counters[transaction.type] = Math.max(
        counters[transaction.type],
        numeric,
      );
    }
  }

  const seen = new Set();
  for (const transaction of transactions) {
    const key = transaction.id.toLowerCase();
    if (transaction.id && !seen.has(key)) {
      seen.add(key);
      continue;
    }
    counters[transaction.type] += 1;
    transaction.id = `${transaction.type === 'income' ? 'inc' : 'exp'}-${String(
      counters[transaction.type],
    ).padStart(4, '0')}`;
    seen.add(transaction.id.toLowerCase());
  }
  return transactions;
}

/** Totals, net balance and expense breakdown, optionally for one month. */
function summarize(transactions, month) {
  const selected = month
    ? transactions.filter((transaction) => transaction.date.startsWith(month))
    : transactions;

  let income = 0;
  let expenses = 0;
  let incomeCount = 0;
  let expenseCount = 0;
  const categories = new Map();

  for (const transaction of selected) {
    if (transaction.type === 'income') {
      income += transaction.amount;
      incomeCount += 1;
    } else {
      expenses += transaction.amount;
      expenseCount += 1;
      categories.set(
        transaction.category,
        (categories.get(transaction.category) ?? 0) + transaction.amount,
      );
    }
  }

  const byCategory = [...categories.entries()]
    .map(([category, amount]) => ({
      category,
      amount,
      share: expenses === 0 ? 0 : amount / expenses,
    }))
    .sort((a, b) => b.amount - a.amount || a.category.localeCompare(b.category));

  return {
    month: month ?? null,
    income,
    expenses,
    net: income - expenses,
    byCategory,
    counts: {
      income: incomeCount,
      expenses: expenseCount,
      total: selected.length,
    },
  };
}


/* ------------------------------------------------------------------ *
 * Markdown rendering
 * ------------------------------------------------------------------ */

function escapeCell(value) {
  return String(value ?? '')
    .replace(/\\/g, '\\\\')
    .replace(/\|/g, '\\|')
    .replace(/\s*\r?\n\s*/g, ' ')
    .trim();
}

function renderRow(values) {
  return `| ${values.map(escapeCell).join(' | ')} |`;
}

const TABLE_HEADER = renderRow([
  'ID',
  'Date',
  'Category',
  'Description',
  'Payment method',
  'Amount',
]);
const TABLE_SEPARATOR = '| --- | --- | --- | --- | --- | ---: |';

function renderTransactionSection(title, transactions) {
  const lines = [`## ${title}`, ''];
  if (transactions.length === 0) {
    lines.push(`_No ${title.toLowerCase()} recorded yet._`);
    return lines;
  }
  lines.push(TABLE_HEADER, TABLE_SEPARATOR);
  for (const transaction of transactions) {
    lines.push(
      renderRow([
        transaction.id,
        transaction.date,
        transaction.category,
        transaction.description,
        transaction.method,
        formatAmountPlain(transaction.amount),
      ]),
    );
  }
  return lines;
}

function renderMarkdown(doc, now = new Date()) {
  const currency = normalizeCurrency(doc.currency ?? DEFAULT_CURRENCY);
  const transactions = [...doc.transactions].sort(compareTransactions);
  const expenses = transactions.filter((t) => t.type === 'expense');
  const incomes = transactions.filter((t) => t.type === 'income');
  const overall = summarize(transactions);
  const month = currentMonth(now);
  const thisMonth = summarize(transactions, month);

  const lines = [
    '# Expense Tracker',
    '',
    '<!-- Generated by the ExpenseTracker CLI. You can edit this file by hand,',
    '     but keep the "## Expenses" / "## Income" tables (including the ID',
    '     column) intact so entries can be read back safely. -->',
    '',
    `- **Currency:** ${currency}`,
    `- **Last updated:** ${formatTimestamp(now)}`,
    `- **Entries:** ${transactions.length} (${expenses.length} expenses, ${incomes.length} income)`,
    '',
    '## Summary',
    '',
    '| Metric | Amount |',
    '| --- | ---: |',
    `| Total income | ${formatMoney(overall.income, currency)} |`,
    `| Total expenses | ${formatMoney(overall.expenses, currency)} |`,
    `| Net balance | ${formatMoney(overall.net, currency)} |`,
    `| Income this month (${month}) | ${formatMoney(thisMonth.income, currency)} |`,
    `| Expenses this month (${month}) | ${formatMoney(thisMonth.expenses, currency)} |`,
    `| Net this month (${month}) | ${formatMoney(thisMonth.net, currency)} |`,
    '',
    '### Spending by category (all time)',
    '',
  ];

  if (overall.byCategory.length === 0) {
    lines.push('_No expenses recorded yet._');
  } else {
    lines.push('| Category | Amount | Share of expenses |', '| --- | ---: | ---: |');
    for (const { category, amount, share } of overall.byCategory) {
      lines.push(
        renderRow([
          category,
          formatMoney(amount, currency),
          `${(share * 100).toFixed(1)}%`,
        ]),
      );
    }
  }

  lines.push('', ...renderTransactionSection('Expenses', expenses));
  lines.push('', ...renderTransactionSection('Income', incomes));
  lines.push('');
  return lines.join('\n');
}


/* ------------------------------------------------------------------ *
 * Markdown parsing
 * ------------------------------------------------------------------ */

/** Split a markdown table row on unescaped pipes. */
function splitRow(line) {
  const trimmed = line.trim().replace(/^\|/, '').replace(/\|\s*$/, '');
  const cells = [];
  let current = '';
  let escaped = false;
  for (const char of trimmed) {
    if (escaped) {
      current += char;
      escaped = false;
      continue;
    }
    if (char === '\\') {
      escaped = true;
      continue;
    }
    if (char === '|') {
      cells.push(current.trim());
      current = '';
      continue;
    }
    current += char;
  }
  cells.push(current.trim());
  return cells;
}

/** Return the text between "## <heading>" and the next "## " heading. */
function extractSection(text, heading) {
  const headingRegex = new RegExp(`^##[ \\t]+${heading}[ \\t]*$`, 'im');
  const match = headingRegex.exec(text);
  if (!match) return '';
  const rest = text.slice(match.index + match[0].length);
  const next = /^##[ \t]+/m.exec(rest);
  return next ? rest.slice(0, next.index) : rest;
}

/** Parse the first markdown table in a section into objects keyed by header. */
function parseTable(sectionText) {
  const lines = sectionText
    .split(/\r?\n/)
    .filter((line) => line.trim().startsWith('|'));
  if (lines.length < 2) return [];

  const headers = splitRow(lines[0]).map((header) =>
    header.toLowerCase().replace(/\s+/g, ' ').replace(/^#/, '').trim(),
  );

  const rows = [];
  for (const line of lines.slice(1)) {
    const cells = splitRow(line);
    if (cells.every((cell) => /^:?-{3,}:?$/.test(cell))) continue; // separator
    const row = {};
    headers.forEach((header, index) => {
      row[header] = cells[index] ?? '';
    });
    if (Object.values(row).some((value) => value !== '')) rows.push(row);
  }
  return rows;
}


function parseMarkdown(text) {
  const warnings = [];
  const currencyMatch = /^-\s+\*\*Currency:\*\*\s*([A-Za-z]{3})\s*$/m.exec(text);
  const currency = currencyMatch ? currencyMatch[1].toUpperCase() : DEFAULT_CURRENCY;

  const transactions = [];
  for (const [heading, type] of [
    ['Expenses', 'expense'],
    ['Income', 'income'],
  ]) {
    for (const row of parseTable(extractSection(text, heading))) {
      try {
        transactions.push({
          id: row.id || '',
          type,
          date: parseDate(row.date),
          category: row.category || 'Other',
          description: row.description || '',
          method:
            row['payment method'] ||
            row.method ||
            (type === 'income' ? 'Bank transfer' : 'Card'),
          amount: parseAmountToCents(row.amount),
        });
      } catch (error) {
        warnings.push(
          `Skipped invalid ${type} row (${row.id || 'no id'}): ${error.message}`,
        );
      }
    }
  }

  return { currency, transactions: assignMissingIds(transactions), warnings };
}

/* ------------------------------------------------------------------ *
 * Storage API
 * ------------------------------------------------------------------ */

function load(file = DEFAULT_FILE) {
  const resolved = path.resolve(file);
  if (!fs.existsSync(resolved)) {
    return {
      file: resolved,
      currency: DEFAULT_CURRENCY,
      transactions: [],
      warnings: [],
    };
  }
  return { file: resolved, ...parseMarkdown(fs.readFileSync(resolved, 'utf8')) };
}

function save(file, doc) {
  const resolved = path.resolve(file);
  fs.mkdirSync(path.dirname(resolved), { recursive: true });
  fs.writeFileSync(resolved, renderMarkdown(doc), 'utf8');
  return resolved;
}

/** Create the tracker file (or update its currency) without losing data. */
function init(file = DEFAULT_FILE, { currency } = {}) {
  const doc = load(file);
  if (currency) doc.currency = normalizeCurrency(currency);
  save(file, doc);
  return load(file);
}

function addTransaction(file, input) {
  const doc = load(file);
  const transaction = normalizeTransaction(input);
  transaction.id = nextId(doc.transactions, transaction.type);
  doc.transactions.push(transaction);
  save(file, doc);
  return transaction;
}

function deleteTransaction(file, id) {
  const doc = load(file);
  const key = String(id ?? '').trim().toLowerCase();
  const index = doc.transactions.findIndex(
    (transaction) => transaction.id.toLowerCase() === key,
  );
  if (index === -1) {
    throw new Error(`No transaction found with ID "${id}".`);
  }
  const [removed] = doc.transactions.splice(index, 1);
  save(file, doc);
  return removed;
}

module.exports = {
  DEFAULT_FILE,
  DEFAULT_CURRENCY,
  CATEGORIES,
  PAYMENT_METHODS,
  TYPE_LABELS,
  addTransaction,
  assignMissingIds,
  currentMonth,
  deleteTransaction,
  formatAmountPlain,
  formatMoney,
  init,
  load,
  nextId,
  normalizeCurrency,
  normalizeMonth,
  parseAmountToCents,
  parseDate,
  parseMarkdown,
  renderMarkdown,
  save,
  summarize,
  todayISO,
  validateCents,
};

