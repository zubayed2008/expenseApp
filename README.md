# Expense Tracker

A zero-dependency command-line expense tracker that stores everything in a
**human-readable Markdown file** (`expenses.md`). It tracks **expenses** as well
as **salary and other income**, keeps a live summary (totals, net balance,
spending by category), and — when run with no arguments — **asks you questions**
interactively to record each transaction.

## Features

- 📝 **Interactive prompts** — the app asks for amount, category, description,
  date and payment method, then asks you to confirm before saving.
- 💸 **Expenses and income** — salary, freelance, bonuses, refunds and more.
- 📄 **Markdown as the database** — every change is written to `expenses.md`,
  which renders nicely on GitHub and can be edited by hand.
- 📊 **Live summary** — total income/expenses, net balance, this-month totals
  and a spending-by-category breakdown are regenerated on every save.
- 🧾 **Scriptable CLI** — `add`, `list`, `summary` and `delete` commands for
  automation, with `--month` / `--type` filters.
- 🔒 **Safe parsing** — invalid hand-edits are skipped with warnings instead of
  crashing, and rows added by hand without an ID get one assigned.
- 0️⃣ **No dependencies** — runs on any Node.js ≥ 18 install.

## Quick start

```bash
# From the repository root (no npm install needed — zero dependencies)
node bin/expense-tracker.js init --currency USD
node bin/expense-tracker.js          # interactive menu — asks you questions
```

Or install the `expense-tracker` command globally:

```bash
npm link
expense-tracker
```

## Interactive usage

Running the app with no arguments opens a menu that asks questions:

```text
===========================================
  Expense Tracker
  Data file: /path/to/expenses.md
  Currency:  USD   Entries: 2
===========================================
  1) Add an expense
  2) Add income / salary
  3) View transactions
  4) Show summary
  5) Delete a transaction
  6) Quit

Choose an option [1-6]: 1

New expense — press Enter to accept the [default].
Amount (USD): 45.20

Category:
  1) Food
  2) Groceries
  ...
Pick a number or type your own [1]: 1
Description (optional): Weekly groceries
Date [today]:
...
Save this expense? $45.20 · Food · 2026-10-20 · Card [Y/n]:
✔ Saved as exp-0001.
```

Pressing **Enter** accepts the default shown in brackets; dates accept
`YYYY-MM-DD`, `today` or `yesterday`, and categories can be picked by number
or typed freely (e.g. your own custom category).

## Non-interactive commands

```bash
# Add an expense
node bin/expense-tracker.js add expense \
  --amount 45.20 --category Food --description "Weekly groceries" \
  --date 2026-10-20 --method Card

# Add income / salary
node bin/expense-tracker.js add income \
  --amount 5000 --category Salary --description "October salary"

# List transactions (optional filters)
node bin/expense-tracker.js list --month 2026-10 --type expense --limit 20

# Summaries
node bin/expense-tracker.js summary                 # all time + this month
node bin/expense-tracker.js summary --month 2026-10

# Delete by ID (IDs are shown by `list`)
node bin/expense-tracker.js delete exp-0001

# Use a different data file
node bin/expense-tracker.js list --file ~/Documents/money.md
# or: EXPENSE_TRACKER_FILE=~/Documents/money.md node bin/expense-tracker.js
```

## The markdown data file

`expenses.md` is the single source of truth. Example after a few entries:

```markdown
# Expense Tracker

- **Currency:** USD
- **Last updated:** 2026-10-20 09:41
- **Entries:** 2 (1 expenses, 1 income)

## Summary

| Metric | Amount |
| --- | ---: |
| Total income | $5,000.00 |
| Total expenses | $45.20 |
| Net balance | $4,954.80 |
...

## Expenses

| ID | Date | Category | Description | Payment method | Amount |
| --- | --- | --- | --- | --- | ---: |
| exp-0001 | 2026-10-20 | Food | Weekly groceries | Cash | 45.20 |

## Income

| ID | Date | Category | Description | Payment method | Amount |
| --- | --- | --- | --- | --- | ---: |
| inc-0001 | 2026-10-20 | Salary | October salary | Bank transfer | 5000.00 |
```

You can edit this file by hand (fix a typo, add a row) — keep the `## Expenses`
and `## Income` table headers intact, and the summary will be regenerated the
next time the app saves. Amounts are stored internally as integer cents, so
totals never suffer from floating-point drift.

## Project structure

```text
bin/expense-tracker.js   # executable entry point
src/tracker.js           # core: validation, markdown parse/render, summaries
src/cli.js               # interactive menu + non-interactive commands
test/tracker.test.js     # unit tests (node:test)
test/cli.test.js         # end-to-end CLI tests
expenses.md              # your data file
```

## Development

```bash
npm test     # run the test suite (node --test)
npm start    # same as: node bin/expense-tracker.js
```

## Roadmap ideas

- [ ] Edit an existing transaction
- [ ] Monthly budgets per category with warnings
- [ ] CSV import/export
- [ ] Recurring transactions (e.g. monthly salary)
- [ ] Web or mobile UI on top of the same markdown file
