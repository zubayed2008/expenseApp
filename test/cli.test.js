'use strict';

const test = require('node:test');
const assert = require('node:assert/strict');
const { execFileSync } = require('node:child_process');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');

const BIN = path.join(__dirname, '..', 'bin', 'expense-tracker.js');

function tempFile(t) {
  const dir = fs.mkdtempSync(path.join(os.tmpdir(), 'expense-tracker-cli-'));
  t.after(() => fs.rmSync(dir, { recursive: true, force: true }));
  return path.join(dir, 'expenses.md');
}

function cli(t, args) {
  const file = tempFile(t);
  const run = (extra) =>
    execFileSync(process.execPath, [BIN, ...extra, '--file', file], {
      encoding: 'utf8',
    });
  return { file, run };
}

test('cli: init, add, list, summary and delete work end to end', (t) => {
  const { file, run } = cli(t);

  assert.match(run(['init', '--currency', 'USD']), /Tracker ready/);
  assert.ok(fs.existsSync(file));

  const addOut = run([
    'add', 'expense',
    '--amount', '12.50',
    '--category', 'Food',
    '--description', 'Lunch',
    '--date', '2026-10-20',
  ]);
  assert.match(addOut, /Added expense exp-0001/);

  run([
    'add', 'income',
    '--amount', '3000',
    '--category', 'Salary',
    '--date', '2026-10-01',
  ]);

  const list = run(['list']);
  assert.match(list, /exp-0001/);
  assert.match(list, /inc-0001/);
  assert.match(list, /Lunch/);
  assert.match(list, /Salary/);

  const expensesOnly = run(['list', '--type', 'expense']);
  assert.match(expensesOnly, /exp-0001/);
  assert.doesNotMatch(expensesOnly, /inc-0001/);

  const summary = run(['summary']);
  assert.match(summary, /Summary — all time/);
  assert.match(summary, /Net/);
  assert.match(summary, /Spending by category/);

  const monthSummary = run(['summary', '--month', '2026-09']);
  assert.match(monthSummary, /Summary for 2026-09/);
  assert.match(monthSummary, /Entries\s+0/);

  const del = run(['delete', 'exp-0001']);
  assert.match(del, /Deleted exp-0001/);
  assert.doesNotMatch(run(['list']), /exp-0001/);
});

test('cli: add without --amount fails clearly when non-interactive', (t) => {
  const { run } = cli(t);
  assert.throws(() => run(['add', 'expense']), (error) =>
    /--amount is required/.test(String(error.stderr)),
  );
});

test('cli: unknown commands print help and set a failing exit code', (t) => {
  const { run } = cli(t);
  assert.throws(
    () => run(['frobnicate']),
    (error) => error.status === 1 && /Unknown command/.test(String(error.stdout)),
  );
});
