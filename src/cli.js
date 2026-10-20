'use strict';

const path = require('node:path');
const readline = require('node:readline/promises');
const { stdin, stdout } = require('node:process');

const tracker = require('./tracker');

/* ------------------------------------------------------------------ *
 * Helpers
 * ------------------------------------------------------------------ */

function parseArgs(argv) {
  const flags = {};
  const positional = [];
  let command;
  for (let index = 0; index < argv.length; index += 1) {
    const arg = argv[index];
    if (arg.startsWith('--')) {
      const body = arg.slice(2);
      if (body.includes('=')) {
        const [key, ...value] = body.split('=');
        flags[key] = value.join('=');
      } else if (index + 1 < argv.length && !argv[index + 1].startsWith('--')) {
        flags[body] = argv[index + 1];
        index += 1;
      } else {
        flags[body] = true;
      }
    } else if (command === undefined && positional.length === 0) {
      command = arg;
    } else {
      positional.push(arg);
    }
  }
  return { command, positional, flags };
}

function resolveFile(flags) {
  if (flags.file && typeof flags.file !== 'string') {
    throw new Error('--file needs a path, for example --file data/expenses.md');
  }
  const candidate =
    flags.file || process.env.EXPENSE_TRACKER_FILE || tracker.DEFAULT_FILE;
  return path.resolve(candidate);
}

function printTransactions(transactions, currency) {
  if (transactions.length === 0) {
    console.log('No transactions found.');
    return;
  }
  const headers = ['ID', 'Date', 'Type', 'Category', 'Description', 'Method', 'Amount'];
  const rows = transactions.map((t) => [
    t.id,
    t.date,
    tracker.TYPE_LABELS[t.type] ?? t.type,
    t.category,
    t.description || '-',
    t.method || '-',
    tracker.formatMoney(t.amount, currency),
  ]);
  const widths = headers.map((header, index) =>
    Math.max(header.length, ...rows.map((row) => String(row[index]).length)),
  );
  const line = (cells) =>
    cells
      .map((cell, index) =>
        index === cells.length - 1
          ? String(cell).padStart(widths[index])
          : String(cell).padEnd(widths[index]),
      )
      .join('  ');
  console.log(line(headers));
  console.log(widths.map((width) => '-'.repeat(width)).join('  '));
  for (const row of rows) console.log(line(row));
}

function printSummaryBlock(summary, currency, title) {
  const money = (cents) => tracker.formatMoney(cents, currency);
  console.log(`\n${title} (${currency})`);
  console.log(`  Income    ${money(summary.income)}`);
  console.log(`  Expenses  ${money(summary.expenses)}`);
  console.log(`  Net       ${money(summary.net)}`);
  console.log(`  Entries   ${summary.counts.total} (${summary.counts.expenses} expenses, ${summary.counts.income} income)`);
  if (summary.byCategory.length > 0) {
    console.log('  Spending by category:');
    for (const { category, amount, share } of summary.byCategory) {
      console.log(
        `    ${category.padEnd(16)} ${money(amount).padStart(12)}  ${(share * 100).toFixed(1)}%`,
      );
    }
  }
}

function printSummary(doc, month) {
  if (month) {
    const summary = tracker.summarize(doc.transactions, month);
    printSummaryBlock(summary, doc.currency, `Summary for ${month}`);
    return;
  }
  printSummaryBlock(tracker.summarize(doc.transactions), doc.currency, 'Summary — all time');
  const current = tracker.currentMonth();
  printSummaryBlock(
    tracker.summarize(doc.transactions, current),
    doc.currency,
    `Summary — this month (${current})`,
  );
}

const HELP = `
Expense Tracker — expenses and income stored in a Markdown file

Usage:
  expense-tracker                     Start the interactive menu (asks questions)
  expense-tracker init [--currency USD]
  expense-tracker add <expense|income> --amount 12.50 --category Food
                                      [--description "..."] [--date today] [--method Card]
  expense-tracker list [--type expense|income] [--month YYYY-MM] [--limit 20]
  expense-tracker summary [--month YYYY-MM]
  expense-tracker delete <id> [<id> ...]

Options:
  --file <path>   Markdown file to use (default ./expenses.md, or EXPENSE_TRACKER_FILE)
  -h, --help      Show this help
`;

/* ------------------------------------------------------------------ *
 * Non-interactive commands
 * ------------------------------------------------------------------ */

function validateType(value) {
  const type = String(value ?? '').trim().toLowerCase();
  if (type !== 'expense' && type !== 'income') {
    throw new Error(
      `Expected "expense" or "income", got "${value}".\nUsage: expense-tracker add <expense|income> --amount 12.50 ...`,
    );
  }
  return type;
}

async function runAdd(file, positional, flags) {
  const type = validateType(positional[0] || flags.type);

  if (flags.amount === undefined) {
    if (!stdin.isTTY) {
      throw new Error('--amount is required in non-interactive mode.');
    }
    const rl = readline.createInterface({ input: stdin, output: stdout });
    try {
      await addTransactionFlow(rl, file, type);
    } finally {
      rl.close();
    }
    return;
  }

  const transaction = tracker.addTransaction(file, {
    type,
    amount: flags.amount,
    category: flags.category,
    description: flags.description,
    date: flags.date,
    method: flags.method,
  });
  const currency = tracker.load(file).currency;
  console.log(
    `Added ${type} ${transaction.id}: ` +
      `${tracker.formatMoney(transaction.amount, currency)} ` +
      `(${transaction.category}) on ${transaction.date}`,
  );
}

function runList(file, flags) {
  const doc = tracker.load(file);
  let transactions = doc.transactions;
  if (flags.month) {
    const month = tracker.normalizeMonth(flags.month);
    transactions = transactions.filter((t) => t.date.startsWith(month));
  }
  if (flags.type) {
    const type = validateType(flags.type);
    transactions = transactions.filter((t) => t.type === type);
  }
  transactions = [...transactions].sort(
    (a, b) => b.date.localeCompare(a.date) || b.id.localeCompare(a.id),
  );
  const limit = Number(flags.limit ?? 0);
  if (Number.isInteger(limit) && limit > 0) {
    transactions = transactions.slice(0, limit);
  }
  printTransactions(transactions, doc.currency);
  for (const warning of doc.warnings) console.warn(`Warning: ${warning}`);
}

function runSummary(file, flags) {
  const doc = tracker.load(file);
  if (doc.transactions.length === 0) {
    console.log(`No transactions yet. Add one with:\n  expense-tracker add expense --amount 12.50 --category Food --file ${doc.file}`);
    return;
  }
  printSummary(doc, flags.month ? tracker.normalizeMonth(flags.month) : null);
}

function runDelete(file, positional, flags) {
  const ids = [...positional, ...(flags.id ? [String(flags.id)] : [])];
  if (ids.length === 0) {
    throw new Error('Usage: expense-tracker delete <id> [<id> ...]');
  }
  for (const id of ids) {
    const removed = tracker.deleteTransaction(file, id);
    console.log(`Deleted ${removed.id} (${removed.type}, ${removed.date}).`);
  }
}

/* ------------------------------------------------------------------ *
 * Interactive menu (the app asks questions)
 * ------------------------------------------------------------------ */

async function askValid(rl, question, parse) {
  for (;;) {
    const answer = (await rl.question(question)).trim();
    try {
      return parse(answer);
    } catch (error) {
      console.log(`  ${error.message}`);
    }
  }
}

async function askChoice(rl, label, options, defaultIndex = 0) {
  console.log(`\n${label}:`);
  options.forEach((option, index) => console.log(`  ${index + 1}) ${option}`));
  for (;;) {
    const answer = (await rl.question(`Pick a number or type your own [${defaultIndex + 1}]: `)).trim();
    if (!answer) return options[defaultIndex];
    if (/^\d+$/.test(answer)) {
      const index = Number(answer) - 1;
      if (index >= 0 && index < options.length) return options[index];
      console.log(`Please choose between 1 and ${options.length}.`);
      continue;
    }
    return answer;
  }
}

async function addTransactionFlow(rl, file, type) {
  const doc = tracker.load(file);
  console.log(`\nNew ${type} — press Enter to accept the [default].`);

  const amountCents = await askValid(rl, `Amount (${doc.currency}): `, (value) =>
    tracker.parseAmountToCents(value),
  );
  const category = await askChoice(rl, 'Category', tracker.CATEGORIES[type]);
  const description = (await rl.question('Description (optional): ')).trim();
  const date = await askValid(rl, 'Date [today]: ', (value) =>
    tracker.parseDate(value || 'today'),
  );
  const defaultMethodIndex = type === 'income'
    ? tracker.PAYMENT_METHODS.indexOf('Bank transfer')
    : tracker.PAYMENT_METHODS.indexOf('Card');
  const method = await askChoice(rl, 'Payment method', tracker.PAYMENT_METHODS, defaultMethodIndex);

  const preview = [
    `${tracker.formatMoney(amountCents, doc.currency)}`,
    category,
    date,
    method,
  ].join(' · ');
  const confirm = (await rl.question(`\nSave this ${type}? ${preview} [Y/n]: `))
    .trim()
    .toLowerCase();
  if (confirm === 'n' || confirm === 'no') {
    console.log('Cancelled.');
    return;
  }

  const transaction = tracker.addTransaction(file, {
    type,
    amountCents,
    category,
    description,
    date,
    method,
  });
  console.log(`✔ Saved as ${transaction.id}.`);
}


async function viewFlow(rl, file) {
  const doc = tracker.load(file);
  if (doc.transactions.length === 0) {
    console.log('\nNo transactions yet. Choose option 1 or 2 to add one.');
    return;
  }
  const period = await askChoice(
    rl,
    'Which transactions?',
    ['This month', 'Last 30 days', 'All time'],
    0,
  );
  let transactions = doc.transactions;
  if (period === 'This month') {
    const month = tracker.currentMonth();
    transactions = transactions.filter((t) => t.date.startsWith(month));
  } else if (period === 'Last 30 days') {
    const cutoff = new Date();
    cutoff.setDate(cutoff.getDate() - 30);
    const since = tracker.todayISO(cutoff);
    transactions = transactions.filter((t) => t.date >= since);
  }
  console.log('');
  printTransactions(
    [...transactions].sort(
      (a, b) => b.date.localeCompare(a.date) || b.id.localeCompare(a.id),
    ),
    doc.currency,
  );
}

async function deleteFlow(rl, file) {
  const doc = tracker.load(file);
  if (doc.transactions.length === 0) {
    console.log('\nNothing to delete yet.');
    return;
  }
  const recent = [...doc.transactions]
    .sort((a, b) => b.date.localeCompare(a.date) || b.id.localeCompare(a.id))
    .slice(0, 10);
  console.log('\nMost recent transactions:');
  printTransactions(recent, doc.currency);

  const id = (await rl.question('\nID to delete (blank to cancel): ')).trim();
  if (!id) return;
  const target = doc.transactions.find(
    (t) => t.id.toLowerCase() === id.toLowerCase(),
  );
  if (!target) {
    console.log(`No transaction found with ID "${id}".`);
    return;
  }
  const confirm = (await rl.question(
    `Delete ${target.id} (${tracker.formatMoney(target.amount, doc.currency)}, ${target.category}, ${target.date})? [y/N]: `,
  ))
    .trim()
    .toLowerCase();
  if (confirm !== 'y' && confirm !== 'yes') {
    console.log('Cancelled.');
    return;
  }
  tracker.deleteTransaction(file, id);
  console.log(`✔ Deleted ${target.id}.`);
}

async function runInteractive(file) {
  if (!stdin.isTTY) {
    console.log(HELP);
    return;
  }
  if (!require('node:fs').existsSync(file)) {
    tracker.init(file);
    console.log(`Created ${file}\n`);
  }

  const rl = readline.createInterface({ input: stdin, output: stdout });
  rl.on('SIGINT', () => {
    console.log('\nGoodbye!');
    rl.close();
    process.exit(0);
  });

  try {
    let running = true;
    while (running) {
      const doc = tracker.load(file);
      console.log('\n===========================================');
      console.log('  Expense Tracker');
      console.log(`  Data file: ${doc.file}`);
      console.log(`  Currency:  ${doc.currency}   Entries: ${doc.transactions.length}`);
      console.log('===========================================');
      console.log('  1) Add an expense');
      console.log('  2) Add income / salary');
      console.log('  3) View transactions');
      console.log('  4) Show summary');
      console.log('  5) Delete a transaction');
      console.log('  6) Quit');

      const choice = (await rl.question('\nChoose an option [1-6]: ')).trim().toLowerCase();
      try {
        switch (choice) {
          case '1':
          case 'expense':
            await addTransactionFlow(rl, file, 'expense');
            break;
          case '2':
          case 'income':
            await addTransactionFlow(rl, file, 'income');
            break;
          case '3':
          case 'list':
            await viewFlow(rl, file);
            break;
          case '4':
          case 'summary':
            if (doc.transactions.length === 0) {
              console.log('\nNo transactions yet.');
            } else {
              printSummary(tracker.load(file), null);
            }
            break;
          case '5':
          case 'delete':
            await deleteFlow(rl, file);
            break;
          case '6':
          case 'q':
          case 'quit':
          case 'exit':
            running = false;
            break;
          default:
            console.log('Please choose an option from 1 to 6.');
        }
      } catch (error) {
        console.log(`  Error: ${error.message}`);
      }
    }
    console.log('Goodbye!');
  } finally {
    rl.close();
  }
}

/* ------------------------------------------------------------------ *
 * Entry point
 * ------------------------------------------------------------------ */

async function run(argv) {
  const { command, positional, flags } = parseArgs(argv);

  if (flags.help || command === 'help' || command === '--help' || command === '-h') {
    console.log(HELP);
    return;
  }

  const file = resolveFile(flags);

  switch (command) {
    case undefined:
    case 'menu':
    case 'interactive':
      await runInteractive(file);
      return;
    case 'init':
      tracker.init(file, { currency: flags.currency });
      console.log(`Tracker ready: ${file}`);
      return;
    case 'add':
      await runAdd(file, positional, flags);
      return;
    case 'list':
      runList(file, flags);
      return;
    case 'summary':
      runSummary(file, flags);
      return;
    case 'delete':
      runDelete(file, positional, flags);
      return;
    default:
      console.log(`Unknown command: ${command}\n`);
      console.log(HELP);
      process.exitCode = 1;
  }
}

module.exports = { run, parseArgs, printTransactions, printSummary };

