'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');

const tracker = require('../src/tracker');

function tempFile(t) {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'expense-tracker-'));
  t.after(() => fs.rmSync(dir, { recursive: true, force: true }));
  return path.join(dir, 'expenses.md');
}

test('parseAmountToCents handles common money formats', () => {
  assert.equal(tracker.parseAmountToCents('12'), 1200);
  assert.equal(tracker.parseAmountToCents('12.5'), 1250);
  assert.equal(tracker.parseAmountToCents(' 1,234.56 '), 123456);
  assert.equal(tracker.parseAmountToCents('$9.99'), 999);
  assert.equal(tracker.parseAmountToCents(19.99), 1999);
});

test('parseAmountToCents rejects invalid values', () => {
  assert.throws(() => tracker.parseAmountToCents('abc'), /Invalid amount/);
  assert.throws(() => tracker.parseAmountToCents('12.345'), /Invalid amount/);
  assert.throws(() => tracker.parseAmountToCents('-5'), /Invalid amount/);
  assert.throws(() => tracker.parseAmountToCents(0), /greater than zero/);
  assert.throws(() => tracker.parseAmountToCents('0.00'), /greater than zero/);
});

test('parseDate accepts ISO dates, today and yesterday', () => {
  assert.equal(tracker.parseDate('2026-10-20'), '2026-10-20');
  assert.equal(tracker.parseDate(''), tracker.todayISO());
  assert.equal(tracker.parseDate('today'), tracker.todayISO());
  const yesterday = new Date();
  yesterday.setDate(yesterday.getDate() - 1);
  assert.equal(tracker.parseDate('yesterday'), tracker.todayISO(yesterday));
  assert.throws(() => tracker.parseDate('2026-13-01'), /Invalid date/);
  assert.throws(() => tracker.parseDate('2026-02-30'), /does not exist/);
  assert.throws(() => tracker.parseDate('20/10/2026'), /Invalid date/);
});

test('init creates the markdown file and persists currency', (t) => {
  const file = tempFile(t);
  tracker.init(file, { currency: 'GBP' });
  const text = fs.readFileSync(file, 'utf8');
  assert.match(text, /# Expense Tracker/);
  assert.match(text, /\*\*Currency:\*\* GBP/);
  assert.match(text, /## Expenses/);
  assert.match(text, /## Income/);
  assert.throws(() => tracker.init(file, { currency: 'pounds' }), /Invalid currency/);
});

test('addTransaction writes expenses and income with stable IDs', (t) => {
  const file = tempFile(t);
  const expense = tracker.addTransaction(file, {
    type: 'expense',
    amount: '45.20',
    category: 'Food',
    description: 'Weekly shop',
    date: '2026-10-20',
    method: 'Card',
  });
  const income = tracker.addTransaction(file, {
    type: 'income',
    amount: 5000,
    category: 'Salary',
    description: 'October salary',
    date: '2026-10-01',
    method: 'Bank transfer',
  });
  const expense2 = tracker.addTransaction(file, {
    type: 'expense',
    amountCents: 999,
    category: 'Transport',
    date: '2026-10-21',
  });

  assert.equal(expense.id, 'exp-0001');
  assert.equal(income.id, 'inc-0001');
  assert.equal(expense2.id, 'exp-0002');
  assert.equal(expense.amount, 4520);
  assert.equal(income.amount, 500000);

  const doc = tracker.load(file);
  assert.equal(doc.warnings.length, 0);
  assert.equal(doc.transactions.length, 3);
  const reloaded = doc.transactions.find((t) => t.id === 'inc-0001');
  assert.equal(reloaded.type, 'income');
  assert.equal(reloaded.category, 'Salary');
});

test('descriptions with pipes and newlines round-trip safely', (t) => {
  const file = tempFile(t);
  tracker.addTransaction(file, {
    type: 'expense',
    amount: '10',
    category: 'Food',
    description: 'Dinner | drinks\nthen taxi',
    date: '2026-10-20',
  });
  const text = fs.readFileSync(file, 'utf8');
  assert.match(text, /Dinner \\\| drinks then taxi/);
  const doc = tracker.load(file);
  assert.equal(doc.transactions[0].description, 'Dinner | drinks then taxi');
  assert.equal(doc.warnings.length, 0);
});

test('summarize computes totals, net and category breakdown', (t) => {
  const file = tempFile(t);
  const seed = [
    { type: 'expense', amount: '30', category: 'Food', date: '2026-10-05' },
    { type: 'expense', amount: '20', category: 'Food', date: '2026-10-06' },
    { type: 'expense', amount: '1000', category: 'Rent', date: '2026-09-01' },
    { type: 'income', amount: '3000', category: 'Salary', date: '2026-10-01' },
  ];
  for (const item of seed) tracker.addTransaction(file, item);
  const doc = tracker.load(file);

  const all = tracker.summarize(doc.transactions);
  assert.equal(all.income, 300000);
  assert.equal(all.expenses, 105000);
  assert.equal(all.net, 195000);
  assert.deepEqual(
    all.byCategory.map((c) => c.category),
    ['Rent', 'Food'],
  );
  assert.equal(all.byCategory[0].share, 100000 / 105000);

  const october = tracker.summarize(doc.transactions, '2026-10');
  assert.equal(october.expenses, 5000);
  assert.equal(october.income, 300000);
  assert.equal(october.net, 295000);
  assert.equal(october.byCategory.length, 1);
  assert.equal(october.byCategory[0].category, 'Food');
  assert.equal(october.byCategory[0].share, 1);
});

test('the rendered markdown file contains a live summary', (t) => {
  const file = tempFile(t);
  tracker.addTransaction(file, {
    type: 'expense',
    amount: '1200',
    category: 'Rent',
    date: '2026-10-01',
  });
  tracker.addTransaction(file, {
    type: 'income',
    amount: '3000',
    category: 'Salary',
    date: '2026-10-01',
  });
  const text = fs.readFileSync(file, 'utf8');
  assert.match(text, /\| Total income \| \$3,000\.00 \|/);
  assert.match(text, /\| Total expenses \| \$1,200\.00 \|/);
  assert.match(text, /\| Net balance \| \$1,800\.00 \|/);
  assert.match(text, /\| Rent \| \$1,200\.00 \| 100\.0% \|/);
});

test('deleteTransaction removes by ID and errors on unknown IDs', (t) => {
  const file = tempFile(t);
  tracker.addTransaction(file, {
    type: 'expense',
    amount: '10',
    category: 'Food',
    date: '2026-10-20',
  });
  const removed = tracker.deleteTransaction(file, 'EXP-0001'); // case-insensitive
  assert.equal(removed.id, 'exp-0001');
  assert.equal(tracker.load(file).transactions.length, 0);
  assert.throws(() => tracker.deleteTransaction(file, 'exp-0001'), /No transaction found/);
});

test('hand-edited markdown rows are preserved and invalid rows are skipped', (t) => {
  const file = tempFile(t);
  tracker.addTransaction(file, {
    type: 'expense',
    amount: '10',
    category: 'Food',
    date: '2026-10-20',
  });

  let text = fs.readFileSync(file, 'utf8');
  // Hand-edit the amount and add a row without an ID.
  text = text.replace('| 10.00 |', '| 12.34 |');
  text = text.replace(
    /(\| exp-0001 .*)/,
    '$1\n|  | 2026-10-21 | Coffee | Hand added | Cash | 3.50 |\n| broken row |',
  );
  fs.writeFileSync(file, text);

  const doc = tracker.load(file);
  assert.equal(doc.transactions.length, 2);
  const handAdded = doc.transactions.find((t) => t.description === 'Hand added');
  assert.equal(handAdded.amount, 350);
  assert.ok(handAdded.id, 'missing IDs get assigned on load');
  const edited = doc.transactions.find((t) => t.id === 'exp-0001');
  assert.equal(edited.amount, 1234);
  assert.ok(doc.warnings.some((w) => /Skipped invalid/.test(w)));
});

test('validation rejects bad input before touching the file', (t) => {
  const file = tempFile(t);
  assert.throws(
    () => tracker.addTransaction(file, { type: 'refund', amount: '10' }),
    /Unknown transaction type/,
  );
  assert.throws(
    () => tracker.addTransaction(file, { type: 'expense', amount: '0' }),
    /greater than zero/,
  );
  assert.throws(
    () => tracker.addTransaction(file, { type: 'expense', amount: '10', date: '32 Oct' }),
    /Invalid date/,
  );
  assert.equal(fs.existsSync(file), false);
});

