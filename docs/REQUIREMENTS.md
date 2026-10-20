# Expense Tracker — Software Requirements Specification

| | |
| --- | --- |
| **Product family** | Expense Tracker — REST API, Android client, legacy CLI |
| **Document version** | 2.2 |
| **Date** | 2026-10-20 |
| **Status** | Approved — re-baseline for platform architecture |
| **Companion document** | [Implementation Plan](./IMPLEMENTATION_PLAN.md) |

## Revision history

| Version | Date | Change |
| --- | --- | --- |
| 1.0 | 2026-10-20 | Initial SRS: zero-dependency Node.js CLI storing expenses and income in a Markdown file. Phases 0–1 delivered. |
| 2.0 | 2026-10-20 | Re-baseline to a platform architecture: ASP.NET Core 8 + PostgreSQL backend and a React Native (Android) client. Web UI explicitly excluded. Legacy CLI moves to maintenance mode; v1 Phases 2–4 are superseded by platform Phases 5–9. Domain rules (validation, summaries, budgets, recurring) are carried forward and re-specified below. |
| 2.1 | 2026-10-20 | Phase 5 implementation note: `categories.user_id` is required (not nullable); default categories are seeded per account at registration (FR-223) instead of via global seed rows. Data-model section updated to match the delivered schema. |
| 2.2 | 2026-10-20 | Phase 5 implementation note: the backend is structured as Domain / Service (CQRS) / Infrastructure / WebCore / Api (controllers), per architecture decision. The physical schema is delivered as numbered SQL scripts (`ExpenseTracker.Domain/Migrations/000…003`), applied manually or by the built-in SqlMigrationRunner; EF Core maps to that schema and no longer generates migrations. |

## 1. Introduction

### 1.1 Purpose

This document specifies the functional and non-functional requirements for
the Expense Tracker platform: a REST backend (ASP.NET Core 8 on PostgreSQL)
and a React Native Android client. It is the authoritative reference for
scope, priorities, and acceptance criteria. The companion
[Implementation Plan](./IMPLEMENTATION_PLAN.md) maps requirements to
delivery phases.

### 1.2 Scope

The platform comprises:

1. **REST API** — authentication, transaction, category, budget, recurring,
   and reporting services backed by PostgreSQL.
2. **Android client** — React Native application providing guided
   question-driven entry, lists, and summaries.
3. **Legacy CLI (maintenance mode)** — the delivered Markdown CLI remains
   functional; its data migrates to the platform via a Markdown import
   (FR-280/281). No new CLI features are planned.
4. **Markdown exchange format** — retained as an export/backup format
   (FR-280), preserving the product's plain-text data-ownership story.

A **web UI is explicitly out of scope** (stakeholder decision, 2026-10-20).

### 1.3 Definitions, acronyms, and abbreviations

| Term | Meaning |
| --- | --- |
| API | The ASP.NET Core REST backend |
| Client | The React Native Android application |
| Transaction | A recorded financial event; an expense or an income |
| Data file | Markdown file in the v1 CLI format (import/export exchange format) |
| JWT | JSON Web Token (bearer authentication) |
| EF Core / Npgsql | ORM and PostgreSQL provider used by the API |
| MoSCoW | Priority scale: Must / Should / Could / Won't |

### 1.4 Overview

Section 2 describes architecture and users; Section 3 the functional
requirements; Section 4 the non-functional requirements; Section 5 the data
model; Section 6 explicit exclusions; Section 7 traceability to v1.

## 2. System overview

### 2.1 Architecture

```mermaid
flowchart LR
  A[React Native client<br/>(Android)] -->|HTTPS, JSON, JWT| B[ASP.NET Core 8<br/>REST API]
  B --> C[(PostgreSQL 16<br/>via EF Core / Npgsql)]
  D[Legacy CLI<br/>(maintenance)] -->|Markdown import / export| B
```

The server is the single source of truth. Clients are stateless with respect
to authoritative data; any offline capability (FR-304) syncs back to the API.

### 2.2 Component lifecycle

| Component | Status |
| --- | --- |
| REST API + PostgreSQL | Foundation delivered (Phase 5); core endpoints in Phase 6 |
| React Native Android client | Planned (Phase 7) |
| Budgets, recurring, insights | Planned (Phase 8) |
| Legacy CLI | Delivered, maintenance mode |

### 2.3 Stakeholders and personas

| Persona | Description | Primary needs |
| --- | --- | --- |
| **Mobile user** (primary) | Records spending and earnings on an Android phone | Fast guided entry, clear summary, works on the move |
| **Power user** | Scripting and API access | Documented REST API, OpenAPI spec, tokens |
| **Operator** | Runs the API and database | Deployment docs, backups, monitoring |

### 2.4 Operating environment

- **Client:** Android 8.0 (API 26) or later.
- **API:** ASP.NET Core 8 (LTS), Kestrel behind a TLS-terminating reverse
  proxy, deployable via Docker.
- **Database:** PostgreSQL 16.

### 2.5 Constraints

- **C-1** Backend: .NET 8 LTS with EF Core + Npgsql; no other ORM.
- **C-2** Money: `decimal` in C# mapped to PostgreSQL `numeric(18,2)`;
  floating-point types are prohibited for amounts.
- **C-3** One currency per user account, configured in user settings.
- **C-4** Android only for the client; no web UI this release.
- **C-5** All client–server communication is JSON over HTTPS.
- **C-6** UI language is English for this release.

### 2.6 Assumptions and dependencies

- **A-1** The server is authoritative; multiple devices per account may sync.
- **A-2** One account per person; accounts are not shared.
- **A-3** Existing Markdown data (from the legacy CLI) is migrated once, in
  Phase 6, and the Markdown file then becomes a backup/export artifact.

## 3. Functional requirements

Priorities use MoSCoW. Status reflects the re-baselined plan. Requirement IDs
are new for v2; traceability to v1 IDs is in Section 7.

### 3.1 Users and authentication

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-200 | The API shall support account registration with email and password; passwords shall be stored only as salted hashes. | M | Delivered (Phase 5) |
| FR-201 | The API shall issue short-lived JWT access tokens on login and shall reject expired or invalid tokens. | M | Delivered (Phase 5) |
| FR-202 | All user data shall be isolated per account; no endpoint may return or modify another account's data. | M | Delivered (Phase 5) |
| FR-203 | The user shall be able to set their account currency (ISO-4217, default USD) and change it while no transactions exist. | S | Delivered (Phase 5) |

### 3.2 Transactions and categories

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-220 | The API shall create transactions with type (expense/income), amount, date, category, description, and payment method. | M | Planned (Phase 6) |
| FR-221 | The API shall validate amounts as positive numbers with at most two decimals and shall reject everything else with field-level errors. | M | Planned (Phase 6) |
| FR-222 | The API shall validate dates as real calendar dates (`YYYY-MM-DD`) and shall reject invalid ones. | M | Planned (Phase 6) |
| FR-223 | Categories shall be seeded per account (expense and income lists matching the legacy CLI defaults) and shall support user-created custom categories. | M | Seeding delivered (Phase 5); custom categories in Phase 6 |
| FR-224 | The API shall update an existing transaction by ID (supersedes v1 FR-120). | M | Planned (Phase 6) |
| FR-225 | The API shall delete a transaction by ID. | M | Planned (Phase 6) |
| FR-226 | The API shall list transactions with filters by month, type, and category, sorted most-recent-first, with paging (`limit`/`offset` or cursor). | M | Planned (Phase 6) |
| FR-227 | Every transaction shall carry a stable server-assigned ID and created/updated timestamps. | M | Planned (Phase 6) |
| FR-228 | The API shall expose an OpenAPI specification generated from code. | M | Delivered (Phase 5) |

### 3.3 Summaries and reports

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-240 | The API shall report, for all time and for any requested month: total income, total expenses, net balance, entry counts, and a spending-by-category breakdown with shares. | M | Planned (Phase 6) |
| FR-241 | The API shall provide a monthly series (income, expenses, net) for the last N months for trend display. | S | Planned (Phase 8) |
| FR-242 | Summary responses shall include over-budget warnings for months with configured budgets. | S | Planned (Phase 8) |

### 3.4 Budgets and recurring transactions

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-260 | The user shall be able to set a monthly budget per expense category. | S | Planned (Phase 8) |
| FR-261 | Summaries shall warn when a month's category spending exceeds its budget. | S | Planned (Phase 8) |
| FR-262 | The user shall be able to define recurring transactions (e.g. monthly salary, rent) that are materialized on schedule by the API, exactly once per period. | S | Planned (Phase 8) |

### 3.5 Data portability

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-280 | The API shall export a user's transactions in the established Markdown exchange format (v1 data-file layout). | M | Planned (Phase 6) |
| FR-281 | The API shall import a v1 Markdown data file, migrating legacy CLI data; import shall be atomic (all rows or none) with a per-row error report. | M | Planned (Phase 6) |
| FR-282 | The API shall export transactions as CSV. | C | Planned (Phase 8) |

### 3.6 Android client (React Native)

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-300 | The client shall support account registration, login, and logout, storing tokens securely on-device. | M | Planned (Phase 7) |
| FR-301 | The client shall provide a guided add-transaction flow asking, in order: amount, category (pick or custom), description, date (default: today), and payment method (default: Card / Bank transfer), then confirm before saving. | M | Planned (Phase 7) |
| FR-302 | The client shall show a filterable transaction list (month, type) with amounts formatted in the account currency. | M | Planned (Phase 7) |
| FR-303 | The client shall show a dashboard with monthly income, expenses, net, and spending-by-category breakdown. | M | Planned (Phase 7) |
| FR-304 | The client shall queue entries captured offline and sync them to the API when connectivity returns, surfacing any rejected entries. | S | Planned (Phase 7) |
| FR-305 | The client shall render monthly trend charts from FR-241 data. | C | Planned (Phase 8) |

### 3.7 Legacy CLI (maintenance mode)

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-400 | The legacy CLI shall remain functional against the Markdown format (v1 FR-001–FR-103) and receive bug fixes only. | M | Delivered — maintained |
| FR-401 | No new CLI features shall be developed; new capabilities land in the API/client. | M | Delivered — policy |

### 3.8 Data integrity

| ID | Requirement | Priority | Status |
| --- | --- | --- | --- |
| FR-320 | All domain validation rules of the legacy CLI (amount, date, type, category) shall be enforced server-side by the API. | M | Planned (Phase 6) |
| FR-321 | Write endpoints shall be atomic: a validation failure or error must leave stored data unchanged (database transactions). | M | Planned (Phase 6) |


## 4. Non-functional requirements

| ID | Requirement | Verification |
| --- | --- | --- |
| NFR-010 (Security) | Passwords shall be stored as salted hashes (ASP.NET Identity or Argon2id/bcrypt); no plaintext credentials in logs or storage. | Code review + auth tests |
| NFR-011 (Security) | The API shall serve TLS only; plain HTTP shall be refused in production configuration. | Deployment checklist |
| NFR-012 (Security) | Login and registration shall be rate-limited; OWASP API Top 10 checklist shall be signed off before release. | Pen-test checklist (Phase 9) |
| NFR-013 (Security) | Secrets (connection strings, JWT signing key) shall come from environment/configuration, never source control. | Repository scanning |
| NFR-020 (Performance) | Transaction CRUD endpoints shall meet P95 < 300 ms; summary/report endpoints P95 < 1 s with 10,000 transactions per account. | Load test (Phase 9) |
| NFR-030 (Reliability) | Nightly PostgreSQL backups (pg_dump) shall exist, with a documented and rehearsed restore procedure. | Restore drill (Phase 9) |
| NFR-040 (Portability) | The API shall run via Docker with a compose file for local development; the client shall run on Android 8.0+ devices and emulators. | CI + device test matrix |
| NFR-050 (Maintainability) | Backend logic shall be separated from HTTP controllers; behavior shall be covered by xUnit tests including integration tests against real PostgreSQL (Testcontainers). | `dotnet test` suite |
| NFR-051 (Maintainability) | API behavior shall be documented by an always-current OpenAPI spec; CI shall build and test on every push. | CI pipeline |
| NFR-060 (Observability) | The API shall expose health checks and structured logs (Serilog) with request IDs. | Operations review |
| NFR-070 (Usability) | Adding a transaction on the client shall take at most five interactions; every guided input shall state its default. | Manual UX review (Phase 7) |
| NFR-080 (Data safety) | No endpoint write may be partially applied; imports follow all-or-nothing semantics (FR-281). | Integration tests |

## 5. Data model (summary)

Logical model (physical schema delivered as numbered SQL scripts in
`ExpenseTracker.Domain/Migrations` — `000_create_schema_migrations.sql`,
`001_create_users.sql`, `002_create_categories.sql`,
`003_create_transactions.sql` — applied manually or by the built-in
SqlMigrationRunner; applied versions are tracked in `schema_migrations`):

| Table | Key columns | Notes |
| --- | --- | --- |
| `users` | id, email, password_hash, currency, created_at | Currency: ISO-4217, default USD |
| `categories` | id, user_id, name, kind (expense/income) | Seeded per account at registration (FR-223); unique on (user_id, kind, name) |
| `transactions` | id, user_id, type, date, amount numeric(18,2), category_id, description, payment_method, recurring_rule_id (nullable), created_at, updated_at | Index on (user_id, date) |
| `recurring_rules` | id, user_id, type, amount, category_id, day_of_month, active | Materialized by scheduled job |
| `budgets` | id, user_id, category_id, monthly_amount | One active budget per category |

Rules: `amount` is always positive; sign is derived from `type`; money is
`numeric(18,2)` / C# `decimal` only (C-2). Deleting a category with
transactions is refused; budgets cascade only on user deletion.

## 6. Out of scope (v2)

- **Web UI** — explicitly excluded by stakeholder decision (2026-10-20).
- iOS client (Android first; revisit after v2.0).
- New legacy-CLI features (FR-401).
- Bank synchronization / open-banking import.
- Multi-currency conversion within one account.
- Shared/household accounts and collaboration.

## 7. Traceability to v1

| v1 requirement(s) | v2 disposition |
| --- | --- |
| FR-001–FR-007 (interactive entry) | Re-specified as FR-301 (client guided flow) |
| FR-020–FR-022 (non-interactive entry) | Superseded by API (FR-220); scripting via OpenAPI/tokens |
| FR-040–FR-047 (Markdown persistence, IDs, integrity) | Superseded by FR-220–FR-227 + FR-320/321; Markdown retained as exchange format (FR-280/281) |
| FR-060–FR-064 (list, summary, in-file summary) | Re-specified as FR-226, FR-240–FR-242, FR-303 |
| FR-080/081 (delete) | Re-specified as FR-225 |
| FR-100–FR-103 (init, currency, help) | Re-specified as FR-203, FR-228 |
| FR-120 (edit) | Re-specified as FR-224 |
| FR-121 (budgets) | Re-specified as FR-260/261 |
| FR-122 (recurring) | Re-specified as FR-262 |
| FR-123 (CSV) | Re-specified as FR-282 |
| FR-130/131 (trends, charts) | Re-specified as FR-241, FR-305 |
| FR-140 (web UI) | Explicitly out of scope (Section 6) |
| NFR-001–NFR-008 | Carried forward: usability → NFR-070; integrity → FR-320/321, NFR-080; maintainability → NFR-050/051; portability → NFR-040 |

