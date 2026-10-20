# Expense Tracker — Software Requirements Specification

| | |
| --- | --- |
| **Product name** | Expense Tracker |
| **Document version** | 1.0 |
| **Date** | 2026-10-20 |
| **Status** | Approved for development (Phases 0–1 delivered) |
| **Companion document** | [Implementation Plan](./IMPLEMENTATION_PLAN.md) |

## 1. Introduction

### 1.1 Purpose

This document specifies the functional and non-functional requirements for the
Expense Tracker, a personal-finance tool that records expenses and income in a
single, human-readable Markdown file. It is the authoritative reference for
scope, priorities, and acceptance criteria. The companion
[Implementation Plan](./IMPLEMENTATION_PLAN.md) maps these requirements to
delivery phases.

### 1.2 Scope

The product is a zero-dependency command-line application that:

- Records **expenses** and **income** (salary, freelance, bonuses, refunds, etc.)
  through an interactive, question-driven flow or through scriptable commands.
- Persists all data in a **Markdown file** (`expenses.md`) that is legible by
  humans, renders on GitHub, and can be safely edited by hand.
- Maintains a **live summary** (total income, total expenses, net balance,
  current-month totals, and spending-by-category breakdown) that is regenerated
  on every write.

### 1.3 Definitions, acronyms, and abbreviations

| Term | Meaning |
| --- | --- |
| Transaction | A single recorded financial event; either an expense or income |
| Entry | Synonym for transaction |
| Data file | The Markdown file acting as the application's persistent store |
| ID | Stable unique identifier of a transaction (`exp-NNNN` / `inc-NNNN`) |
| Cents | Internal integer representation of money (1 unit = 100 cents) |
| CLI | Command-line interface |
| SRS | Software Requirements Specification |

### 1.4 Overview

Section 2 describes the product context and users. Section 3 lists the
functional requirements. Section 4 lists the non-functional requirements.
Section 5 specifies the data format. Section 6 lists explicit exclusions.

## 2. Product overview

### 2.1 Product perspective

The Expense Tracker is a self-contained Node.js CLI (≥ 18) with no runtime
dependencies. It runs on Linux, macOS, and Windows from the repository checkout
or after `npm link`. All state lives in one Markdown file, so the product fits
naturally into Git-based workflows, plain-text backups, and version diffing.

### 2.2 User characteristics

| Persona | Description | Primary needs |
| --- | --- | --- |
| **Personal user** | Wants a simple daily record of spending and earnings | Fast guided entry, clear summary, zero setup |
| **Power user** | Comfortable with the terminal and scripting | Non-interactive commands, filters, stable IDs |
| **Plain-text enthusiast** | Keeps notes and data in Markdown/Git | Human-editable file, predictable format, diff-friendly history |

### 2.3 Operating environment

- Node.js ≥ 18 on POSIX or Windows.
- A writable filesystem location for the data file (default `./expenses.md`,
  overridable via `--file` or the `EXPENSE_TRACKER_FILE` environment variable).
- An interactive terminal (TTY) for the guided flows; the command set degrades
  gracefully (with clear errors) when no TTY is present.

### 2.4 Constraints

- **C-1** No third-party runtime dependencies.
- **C-2** The data file must remain valid, human-readable Markdown at all times.
- **C-3** Money values must be handled as integer cents to avoid
  floating-point rounding errors.
- **C-4** A single currency applies to the whole data file, configured at
  initialization (`init --currency ISO-4217 code`).

### 2.5 Assumptions and dependencies

- **A-1** The data file is used by one user on one machine at a time.
- **A-2** The user may hand-edit the data file between application runs; the
  application must tolerate and preserve such edits (see FR-110 – FR-113).
- **A-3** Locale formatting (currency symbol, grouping) is provided by the
  JavaScript runtime's `Intl` implementation.

## 3. Functional requirements

Priorities use MoSCoW (M = Must, S = Should, C = Could, W = Won't, this
release). **Status** reflects the current implementation (Phases 0–1).

### 3.1 Data entry — interactive

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-001 | The application shall provide an interactive menu with options to add an expense, add income, view transactions, show a summary, delete a transaction, and quit. | M | Delivered |
| FR-002 | When adding a transaction, the application shall ask, in order: amount, category, description, date, and payment method, accepting Enter for documented defaults. | M | Delivered |
| FR-003 | The date prompt shall accept `YYYY-MM-DD`, `today`, `yesterday`, or blank (default: today) and shall re-prompt with a clear message on invalid input. | M | Delivered |
| FR-004 | The amount prompt shall re-prompt with a clear message on invalid input (non-numeric, negative, zero, or more than two decimal places). | M | Delivered |
| FR-005 | The category prompt shall offer a numbered list of defaults and shall accept a typed custom category. | M | Delivered |
| FR-006 | Before saving, the application shall show the assembled entry for confirmation and shall allow cancellation without writing. | M | Delivered |
| FR-007 | Income entry shall offer distinct default categories (Salary, Bonus, Freelance, Business, Investments, Gifts, Refunds, Other) and a distinct default payment method (Bank transfer). | M | Delivered |

### 3.2 Data entry — non-interactive

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-020 | The application shall support `add <expense\|income>` with `--amount`, `--category`, `--description`, `--date`, and `--method` flags. | M | Delivered |
| FR-021 | When run non-interactively without `--amount`, the application shall fail with a clear error and a non-zero exit code. | M | Delivered |
| FR-022 | `add <type>` without `--amount` on a TTY shall fall back to the interactive flow. | S | Delivered |

### 3.3 Storage and data integrity

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-040 | All transactions shall be persisted to a single Markdown data file containing an Expenses table and an Income table with columns ID, Date, Category, Description, Payment method, Amount. | M | Delivered |
| FR-041 | Each transaction shall receive a stable, unique, human-typeable ID (`exp-NNNN` / `inc-NNNN`) that persists across runs. | M | Delivered |
| FR-042 | Amounts shall be stored internally as integer cents and serialized with exactly two decimals. | M | Delivered |
| FR-043 | The application shall assign IDs to hand-added rows that lack one and shall de-duplicate colliding IDs on load. | S | Delivered |
| FR-044 | The data file shall escape pipes and newlines in free-text fields so tables remain structurally valid. | M | Delivered |
| FR-045 | Invalid rows introduced by hand edits shall be skipped at load time with a warning rather than crashing the application. | M | Delivered |
| FR-046 | Valid hand edits to existing rows (e.g. corrected amounts, added rows) shall be preserved. | M | Delivered |
| FR-047 | The application shall not create or modify the data file when input validation fails. | M | Delivered |


### 3.4 Retrieval and reporting

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-060 | `list` shall display transactions (most recent first) with ID, date, type, category, description, method, and formatted amount. | M | Delivered |
| FR-061 | `list` shall support filtering by month (`--month YYYY-MM`), by type (`--type expense\|income`), and limiting rows (`--limit N`). | M | Delivered |
| FR-062 | `summary` shall report total income, total expenses, net balance, entry counts, and a spending-by-category breakdown with shares. | M | Delivered |
| FR-063 | `summary` shall report all-time figures plus current-month figures by default, and figures for a given month with `--month`. | M | Delivered |
| FR-064 | The data file itself shall contain a regenerated Summary section (totals, net balance, current month, category shares) after every write. | M | Delivered |

### 3.5 Transaction management

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-080 | `delete <id>` shall remove a transaction by ID (case-insensitive); interactive delete shall confirm first. | M | Delivered |
| FR-081 | Deleting an unknown ID shall fail with a clear error and a non-zero exit code. | M | Delivered |

### 3.6 Configuration and help

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-100 | `init` shall create an empty, valid data file and shall set or change the file currency without discarding existing data. | M | Delivered |
| FR-101 | The currency shall be an ISO-4217-style three-letter code persisted in the data file; invalid codes shall be rejected. | M | Delivered |
| FR-102 | The data-file location shall be settable via `--file <path>` or the `EXPENSE_TRACKER_FILE` environment variable. | M | Delivered |
| FR-103 | `--help` and unknown commands shall print complete usage documentation; unknown commands shall exit non-zero. | M | Delivered |

### 3.7 Planned capabilities (Phase 2+)

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-120 | The application shall support editing an existing transaction by ID. | S | Planned (Phase 2) |
| FR-121 | The application shall support monthly budgets per category and shall warn when a budget is exceeded. | S | Planned (Phase 2) |
| FR-122 | The application shall support recurring transactions (e.g. a monthly salary) generated on demand. | S | Planned (Phase 2) |
| FR-123 | The application shall import and export CSV. | C | Planned (Phase 2) |
| FR-130 | The application shall provide trend reports (month-over-month income/expense deltas). | C | Planned (Phase 3) |
| FR-131 | The application shall render category breakdown charts in the terminal. | C | Planned (Phase 3) |
| FR-140 | A web UI shall be provided on top of the same Markdown data format. | W | Phase 4 (revisit) |


## 4. Non-functional requirements

| ID | Requirement | Verification |
| --- | --- | --- |
| NFR-001 (Usability) | Every interactive prompt shall state its default and re-prompt with an actionable message on invalid input. | Manual review of each prompt |
| NFR-002 (Performance) | Any command over a data file with up to 10,000 transactions shall complete within 1 second on commodity hardware. | Load test in Phase 3 |
| NFR-003 (Reliability) | The application shall never write a structurally invalid Markdown file, and shall never lose valid data due to internal serialization. | Automated round-trip tests |
| NFR-004 (Portability) | The application shall run on Node.js ≥ 18 across Linux, macOS, and Windows with no third-party dependencies. | CI matrix (Phase 4) |
| NFR-005 (Maintainability) | Core logic (parsing, validation, summaries) shall be separated from CLI presentation and covered by automated tests. | `node --test` suite |
| NFR-006 (Data safety) | All writes shall be atomic from the user's perspective: a failed validation, unknown ID, or parse error shall leave the file unchanged. | Automated negative tests |
| NFR-007 (Transparency) | All stored data shall remain human-readable plain text with no binary or hidden state. | Inspection of the data file |
| NFR-008 (Observability) | Hand-edit anomalies shall surface as warnings on the console, not silent failures. | Automated warning tests |

## 5. Data format specification

The data file is the single source of truth. Required structure:

1. A `# Expense Tracker` title and metadata bullets: `**Currency:**` (ISO-4217
   code), `**Last updated:**` (write timestamp), `**Entries:**` (counts).
2. A `## Summary` section regenerated by the application on every write
   (users may read but should not rely on hand-written values here).
3. A `### Spending by category (all time)` subsection listing each expense
   category, its formatted total, and its share of expenses.
4. A `## Expenses` section whose table columns are, in order:
   `ID | Date | Category | Description | Payment method | Amount`.
5. A `## Income` section with the same column layout.
6. Dates are `YYYY-MM-DD`; amounts are positive with exactly two decimals;
   pipes and newlines inside free text are escaped as `\|` and spaces.

Additional rules: row order within tables is not significant (rows are sorted
by date, then ID); IDs follow `exp-NNNN` (expenses) and `inc-NNNN` (income)
with four-digit zero padding; sections with no entries hold a placeholder
line (`_No expenses recorded yet._`).

## 6. Out of scope (current release)

- Multi-user or concurrent access (see A-1).
- Multi-currency conversion within one file.
- Bank synchronization / open-banking import.
- Encryption of the data file.
- Mobile or desktop GUI applications (a web UI is re-evaluated in Phase 4).

## 7. Traceability

Every functional requirement above carries a stable ID. The
[Implementation Plan](./IMPLEMENTATION_PLAN.md) references these IDs in each
phase's scope and acceptance criteria, so delivered scope can be audited
requirement-by-requirement.

