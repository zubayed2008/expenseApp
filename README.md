# Expense Tracker

A personal expense tracker that records **expenses** and **salary / other
income**, keeps a live summary (totals, net balance, spending by category),
and **asks you questions** interactively to record each transaction.

## Project direction (v2 — re-baselined 2026-10-20)

The product is moving from a local CLI to a platform architecture:

```text
React Native client (Android)  ──HTTPS, JSON, JWT──▶  ASP.NET Core 8 REST API  ──EF Core / Npgsql──▶  PostgreSQL 16
```

- **Backend:** ASP.NET Core 8 (LTS) + PostgreSQL 16 via EF Core/Npgsql —
  money stored as `numeric(18,2)` (C# `decimal`), JWT auth, OpenAPI.
- **Client:** React Native (TypeScript), Android 8.0+ only. **No web UI** (an
  explicit scoping decision).
- **Legacy CLI:** the delivered Markdown CLI below is in **maintenance mode**
  (bug fixes only). Its data migrates to the platform via a Markdown import,
  and the Markdown format is retained as an export/backup format.

Full details: [Requirements (SRS)](docs/REQUIREMENTS.md) ·
[Implementation Plan](docs/IMPLEMENTATION_PLAN.md)

## Backend (Phase 5 delivered)

Layered ASP.NET Core 8 solution under `backend/` — regular controllers + CQRS:

```text
src/ExpenseTracker.Domain            entities, enums, domain rules, numbered SQL migrations
src/ExpenseTracker.Service           CQRS (MediatR commands/queries + FluentValidation pipeline)
src/ExpenseTracker.Infrastructure    EF Core + Npgsql (maps to the SQL schema), repositories,
                                     bcrypt, JWT, SqlMigrationRunner
src/ExpenseTracker.WebCore           ApiControllerBase, exception→ProblemDetails middleware, auth wiring
src/ExpenseTracker.Api               the host: AuthController, Program.cs, Dockerfile
```

```bash
# Local stack: API on http://localhost:8080 (Swagger at /swagger), Postgres on :5432
docker compose -f backend/docker-compose.yml up --build

# Or run with a local .NET 8 SDK
dotnet run --project backend/src/ExpenseTracker.Api

# Tests (unit tests always run; integration tests use Testcontainers/Docker or
# an existing server via EXPENSE_TRACKER_TEST_CONNECTION)
dotnet test backend/ExpenseTracker.sln
```

**SQL migrations** are numbered files in
`backend/src/ExpenseTracker.Domain/Migrations/` — run them manually in order:

```text
000_create_schema_migrations.sql   (creates the schema_migrations tracking table)
001_create_users.sql
002_create_categories.sql
003_create_transactions.sql
```

Track where you are with
`SELECT version, name, applied_at FROM schema_migrations ORDER BY version;`.
The API can also apply them itself when `Database:ApplySqlMigrations=true`
(used by docker-compose; off in production where you run them manually).

Auth flow: `POST /api/auth/register` (email, password, currency) →
`POST /api/auth/login` (JWT access token) → authenticated requests with
`Authorization: Bearer <token>`. Production configuration comes from
environment variables (`ConnectionStrings__DefaultConnection`, `Jwt__SigningKey`).

## Legacy CLI (delivered, maintenance mode)

A zero-dependency command-line tracker that stores everything in a
**human-readable Markdown file** (`expenses.md`).

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

## Documentation

- [Requirements (SRS) v2](docs/REQUIREMENTS.md) — re-baselined for the platform
  architecture: numbered functional (FR-xxx) and non-functional (NFR-xxx)
  requirements with MoSCoW priorities and status, data model, out-of-scope
  list, and traceability to v1.
- [Implementation Plan v2](docs/IMPLEMENTATION_PLAN.md) — two delivery tracks
  with objectives, deliverables, acceptance criteria, and exit criteria per
  phase, plus risks and change control.

## Roadmap

Delivery is organized in two tracks — see the
[Implementation Plan](docs/IMPLEMENTATION_PLAN.md) for full details:

| Track | Phase | Theme | Status |
| --- | --- | --- | --- |
| CLI | 0 | Foundation & tooling | ✅ Complete |
| CLI | 1 | MVP — core tracking & Markdown persistence | ✅ Complete — maintenance |
| Platform | 5 | Backend & database foundation (ASP.NET Core 8, EF Core, JWT, Docker, CI) | ✅ Complete |
| Platform | 6 | Core API — transactions, summaries, Markdown migration | ⏳ Next |
| Platform | 7 | React Native Android client — MVP | 🗓 Planned |
| Platform | 8 | Budgets, recurring transactions, insights | 🗓 Planned |
| Platform | 9 | Hardening, backups, v2.0 release | 🗓 Planned |
