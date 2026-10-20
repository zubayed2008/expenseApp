# Expense Tracker — Implementation Plan

| | |
| --- | --- |
| **Document version** | 1.0 |
| **Date** | 2026-10-20 |
| **Companion document** | [Requirements (SRS)](./REQUIREMENTS.md) |
| **Delivery approach** | Incremental phases; each phase ships working, tested software |

## 1. Overview

The plan delivers the Expense Tracker in five phases, ordered so that every
phase ends in a usable product and later phases build on a stable core.
Requirement IDs refer to the [SRS](./REQUIREMENTS.md).

| Phase | Theme | Status |
| --- | --- | --- |
| Phase 0 | Foundation & tooling | ✅ Complete |
| Phase 1 | MVP — core tracking & Markdown persistence | ✅ Complete |
| Phase 2 | Data management & robustness | ⏳ Next |
| Phase 3 | Insights, trends & scale | 🗓 Planned |
| Phase 4 | Hardening, distribution & expansion | 🗓 Planned |

---

## Phase 0 — Foundation & tooling ✅

**Objective:** stand up the repository and engineering baseline before any
product features.

**Scope:** project scaffolding; zero-dependency constraint (C-1); test
framework selection (`node:test`, no dev dependencies); coding conventions
(CommonJS, plain functions, strict mode).

**Deliverables**

- [x] `package.json` with `start` / `test` scripts and `bin` entry.
- [x] Executable entry point (`bin/expense-tracker.js`).
- [x] Test layout (`test/`) runnable via `npm test`.
- [x] `.gitignore`, branch and commit conventions.

**Acceptance criteria**

- [x] `node --check` passes on all source files.
- [x] `npm test` runs the (initially minimal) suite green.
- [x] The application prints usage documentation via `--help` (FR-103).

**Exit criteria:** repository builds and tests green with no third-party
packages installed.

---

## Phase 1 — MVP: core tracking & Markdown persistence ✅

**Objective:** deliver the minimum lovable product — record expenses and
income through questions, store them in Markdown, and read them back.

**Scope (requirements)**

- Interactive, question-driven entry flows: FR-001 – FR-007.
- Non-interactive entry: FR-020 – FR-022.
- Markdown storage, IDs, escaping, hand-edit tolerance: FR-040 – FR-047.
- Listing and filtering: FR-060 – FR-061.
- Summaries (CLI and in-file): FR-062 – FR-064.
- Deletion: FR-080 – FR-081.
- Configuration and help: FR-100 – FR-103.
- Non-functional: NFR-001, NFR-003, NFR-005, NFR-006, NFR-007, NFR-008.

**Deliverables**

- [x] `src/tracker.js` — core domain logic (validation, parse/render, summary).
- [x] `src/cli.js` — interactive menu and command surface.
- [x] `expenses.md` — initialized data file.
- [x] `README.md` — user and developer documentation.
- [x] 14 automated unit + CLI end-to-end tests, all passing.

**Acceptance criteria**

- [x] A user can record an expense and a salary through prompts only (Enter
      accepts defaults; invalid input re-prompts).
- [x] All data survives restart and is legible on GitHub as rendered Markdown.
- [x] Hand-edited rows (corrected amounts, added rows) are preserved; broken
      rows are skipped with warnings (FR-043, FR-045, FR-046).
- [x] Every CLI command fails loudly and safely on bad input (NFR-006).
- [x] `npm test` is green.

**Exit criteria:** MVP usable daily by the personal-user persona; no known
data-loss scenarios.

---

## Phase 2 — Data management & robustness ⏳ (next)

**Objective:** make corrections, planning, and migration first-class, so the
tracker supports real monthly budgeting workflows.

**Scope (requirements)**

- Edit an existing transaction by ID: FR-120.
- Monthly budgets per category with over-budget warnings: FR-121.
- Recurring transactions (monthly salary, rent, subscriptions): FR-122.
- CSV import/export: FR-123.

**Deliverables**

- [ ] `edit <id>` command plus an interactive "edit a transaction" flow.
- [ ] Budget model persisted in the Markdown file (new `## Budgets` section)
      with a `budget` command to set/show budgets and warnings in `summary`.
- [ ] Recurring-transaction model (`## Recurring` section) with a `generate`
      command that materializes due entries on demand.
- [ ] `export --csv` and `import --csv` commands with a documented CSV schema
      (date, type, category, description, method, amount) and a dry-run
      preview for imports.
- [ ] Tests for each feature, including budget-warning and import edge cases.

**Acceptance criteria**

- [ ] An entry can be fully edited (or a field updated) without deleting and
      re-adding, and the change is visible in the file and summaries.
- [ ] Setting a budget of X for a category and exceeding it produces a clear
      warning in `summary` output.
- [ ] A monthly salary recurring entry generates exactly one transaction per
      month for the requested range, with stable IDs.
- [ ] A CSV exported and re-imported round-trips to identical transaction
      data; malformed CSV rows are rejected with row-level errors and no
      partial import.

**Exit criteria:** all Phase 2 requirements pass acceptance; no regression in
the existing test suite.

**Effort estimate:** M (three focused work sessions: edit; budgets +
recurring; CSV).

---

## Phase 3 — Insights, trends & scale 🗓

**Objective:** turn recorded data into analysis the user can act on, and
verify the tool at realistic data volumes.

**Scope (requirements)**

- Trend reports, month-over-month deltas: FR-130.
- Terminal category breakdown charts: FR-131.
- Performance verification: NFR-002.

**Deliverables**

- [ ] `report` command: last N months income/expenses/net table with deltas
      and sparkline-style ASCII bars.
- [ ] Category breakdown chart (ASCII bars + share percentages) in `summary`.
- [ ] Load-test harness generating 10,000 transactions and asserting the
      1-second budget for `list`, `summary`, and `add`.

**Acceptance criteria**

- [ ] `report` shows correct deltas between consecutive months.
- [ ] Charts render legibly at 80-column terminals.
- [ ] Performance budget met on the 10k dataset, or bottlenecks documented
      with a follow-up ticket.

**Exit criteria:** insights validated against hand-computed fixtures.

**Effort estimate:** M.

---

## Phase 4 — Hardening, distribution & expansion 🗓

**Objective:** make the tool trustworthy for daily long-term use and decide
on the next form factor.

**Scope**

- Portability matrix (NFR-004) via GitHub Actions CI on Linux/macOS/Windows.
- Packaging and distribution (npm package, `npm install -g` path).
- Data safety: automated backup of the data file on destructive commands,
  with a documented restore procedure.
- Decision point: web UI on the same Markdown data format (FR-140) —
  in scope only after a written proposal and user sign-off.

**Deliverables**

- [ ] CI workflow running `npm test` on the OS/Node matrix.
- [ ] Published npm package with installation docs.
- [ ] Backup/restore documentation and tests.

**Acceptance criteria**

- [ ] CI green on all matrix combinations before every release tag.
- [ ] A fresh machine can install and use the tool within five minutes
      following the README.

**Exit criteria:** v1.0 release tagged from this phase.

**Effort estimate:** S–M.

---

## Risks & mitigations

| Risk | Phase | Mitigation |
| --- | --- | --- |
| Hand edits corrupt table structure | 1+ | Escaping rules, skip-with-warning parsing, load-time ID repair (FR-043 – FR-046) |
| Markdown format becomes too rigid for budgets/recurring | 2 | Extend with new sibling sections; never repurpose existing columns |
| CSV import partially applied on error | 2 | Dry-run validation of the full file before writing anything |
| Performance degrades at scale | 3 | 10k load test in Phase 3 guards NFR-002 before features pile up |
| Web UI forks the data format | 4 | The Markdown file stays the single source of truth for any UI |

## Change control

Changes to requirements or phase scope are made by updating the
[SRS](./REQUIREMENTS.md) and this plan together, bumping both document
versions, and noting the change in the repository commit message.

