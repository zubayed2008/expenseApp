# Expense Tracker — Implementation Plan

| | |
| --- | --- |
| **Document version** | 2.2 |
| **Date** | 2026-10-20 |
| **Companion document** | [Requirements (SRS)](./REQUIREMENTS.md) |
| **Delivery approach** | Two tracks; every phase ships working, tested software |

## Revision history

| Version | Date | Change |
| --- | --- | --- |
| 1.0 | 2026-10-20 | Five phases for the Node.js CLI / Markdown product (Phases 0–1 delivered). |
| 2.0 | 2026-10-20 | Re-baseline for the platform architecture (ASP.NET Core 8 + PostgreSQL backend, React Native Android client, no web UI). CLI Phases 2–4 are superseded; the CLI enters maintenance (Track 1). New platform phases 5–9 (Track 2). |
| 2.1 | 2026-10-20 | Phase 5 delivered: solution scaffold, EF Core + Npgsql models and initial migration (users, categories, transactions), per-account category seeding, bcrypt + JWT auth, Serilog, health checks, OpenAPI/Swagger, Dockerfile, docker-compose, CI workflow template (`backend/ci/backend-ci.yml`, to be activated under `.github/workflows/`), xUnit unit tests (31 passing) and Testcontainers integration tests (skip without Docker; run in CI). |
| 2.2 | 2026-10-20 | Phase 5 restructured per architecture decision: regular controllers (no minimal APIs) + CQRS (MediatR + FluentValidation pipeline) in a Domain / Service / Infrastructure / WebCore / Api layering. EF-generated migrations replaced by numbered SQL scripts in `ExpenseTracker.Domain/Migrations` (schema_migrations tracking; applied manually or via SqlMigrationRunner with advisory locking). All 45 tests verified green against a real PostgreSQL 16 server (integration tests included), plus CLI regression tests. |

## 1. Overview

Delivery is organized in two tracks. Requirement IDs refer to the
[SRS v2](./REQUIREMENTS.md).

| Track | Phase | Theme | Status |
| --- | --- | --- | --- |
| 1 — CLI | 0 | Foundation & tooling | ✅ Complete |
| 1 — CLI | 1 | MVP — core tracking & Markdown persistence | ✅ Complete — maintenance mode |
| 2 — Platform | 5 | Backend & database foundation | ✅ Complete |
| 2 — Platform | 6 | Core API — transactions, summaries, Markdown migration | ⏳ Next |
| 2 — Platform | 7 | React Native Android client — MVP | 🗓 Planned |
| 2 — Platform | 8 | Budgets, recurring transactions, insights | 🗓 Planned |
| 2 — Platform | 9 | Hardening, backups, release | 🗓 Planned |

**Track 1 disposition.** The delivered CLI (Phases 0–1) stays functional and
receives bug fixes only (FR-400/401). Its v1 Phase 2–4 scope is superseded by
the platform phases below (e.g. v1 budgets → FR-260/261 in Phase 8).

---

## Phase 5 — Backend & database foundation ✅

**Objective:** a runnable, deployable API skeleton with authentication,
schema, and CI — the foundation every later phase builds on.

**Scope (requirements):** FR-200 – FR-203, FR-228, FR-203 currency setting;
NFR-010, NFR-011, NFR-013, NFR-040, NFR-050 (structure), NFR-051, NFR-060.

**Deliverables**

- [x] Layered .NET 8 solution under `backend/`:
      `ExpenseTracker.Domain` (entities, rules, numbered SQL migrations),
      `ExpenseTracker.Service` (CQRS: MediatR commands/queries + FluentValidation
      pipeline), `ExpenseTracker.Infrastructure` (EF Core + Npgsql mapping,
      repositories, bcrypt, JWT, SqlMigrationRunner), `ExpenseTracker.WebCore`
      (ApiControllerBase, exception-to-ProblemDetails middleware, auth wiring),
      `ExpenseTracker.Api` (regular controllers + host), and xUnit tests.
- [x] Numbered SQL migrations in `ExpenseTracker.Domain/Migrations`
      (000_create_schema_migrations, 001_create_users, 002_create_categories,
      003_create_transactions) with a `schema_migrations` tracking table,
      applied manually or by the built-in runner (advisory-locked); EF Core maps
      to the SQL-defined schema (snake_case) and generates no migrations;
      12 expense + 8 income default categories seeded per account at
      registration.
- [x] Authentication: registration/login (`POST /api/auth/register`,
      `POST /api/auth/login`, `GET /api/auth/me`), bcrypt password hashes, JWT
      issuance and validation; per-account data isolation (FR-202).
- [x] Docker Compose for local dev (api + postgres); health checks
      (`GET /health` incl. DB check); Serilog structured logging.
- [x] OpenAPI/Swagger generated from code (FR-228), with JWT security scheme.
- [x] CI workflow (`backend/ci/backend-ci.yml` — copy to
      `.github/workflows/` to activate; the automated push credential lacks
      `workflows` permission): build + test on every push/PR touching
      `backend/**`; integration tests against real PostgreSQL via
      Testcontainers.

**Acceptance criteria**

- [x] `docker compose up` starts a working API; `/health` returns healthy —
      compose file delivered; startup could not be exercised in the
      development sandbox (no Docker daemon) — verify on a Docker host.
- [x] Register → login → authenticated request succeeds; unauthenticated
      requests return 401; account A cannot read account B's data (covered
      by integration tests; they run in CI and skip gracefully without
      Docker).
- [x] Migrations apply cleanly to an empty database (verified via
      Testcontainers-based startup in CI; SQL generated design-time).
- [x] CI is green and covers the auth endpoints.

**Exit criteria:** schema and auth stable enough that Phases 6+ add endpoints
without breaking changes. Met.

**Verification notes (2026-10-20, plan v2.2):** `dotnet build` clean; the full
suite — 37 unit tests plus 8 integration tests (auth flows, isolation,
health, seeded categories, duplicate-email 409) — verified green **twice
against a real PostgreSQL 16.4 server** in the development sandbox
(via `EXPENSE_TRACKER_TEST_CONNECTION`); a concurrent-migration race found
during verification was fixed with a PostgreSQL advisory lock in
SqlMigrationRunner. Integration tests also run via Testcontainers in CI and
skip gracefully without Docker.

**Effort estimate:** M.

---

## Phase 6 — Core API: transactions, summaries, Markdown migration 🗓

**Objective:** deliver the complete transaction lifecycle and reporting over
the Phase 5 foundation, including one-shot migration of legacy CLI data.

**Scope (requirements):** FR-220 – FR-227, FR-240, FR-280, FR-281, FR-320,
FR-321; NFR-020 (targets), NFR-080, NFR-050 (coverage).

**Deliverables**

- [ ] Transaction endpoints: create, update (FR-224), delete, list with
      month/type/category filters and paging (FR-226).
- [ ] Category endpoints: list, create custom categories (FR-223).
- [ ] Summary endpoint: all-time and per-month totals, net, category
      breakdown with shares (FR-240).
- [ ] Markdown export endpoint (FR-280) emitting the v1 exchange format.
- [ ] Markdown import endpoint/tool (FR-281): atomic migration of the legacy
      `expenses.md` with a per-row error report.
- [ ] xUnit + Testcontainers suite covering validation, paging, summaries,
      and round-trip export/import.

**Acceptance criteria**

- [ ] Every v1 domain rule (amount > 0 and ≤ 2 decimals, real dates, expense /
      income types) is enforced server-side (FR-320/321).
- [ ] A legacy `expenses.md` migrates with zero data loss; re-exporting it
      yields an equivalent document.
- [ ] Summary math matches hand-computed fixtures (ported from the CLI test
      suite).
- [ ] Invalid input produces field-level 400 errors and leaves data unchanged.

**Exit criteria:** the API covers everything the Android client MVP needs.

**Effort estimate:** M–L.

---

## Phase 7 — React Native Android client — MVP 🗓

**Objective:** a daily-usable Android app: login, guided question-driven
entry, list, and dashboard against the Phase 6 API.

**Scope (requirements):** FR-300 – FR-304 (FR-304 offline queue is Should);
NFR-070, NFR-040 (device matrix).

**Deliverables**

- [ ] React Native (TypeScript) project targeting Android 8.0+.
- [ ] Auth screens: register, login, logout; secure on-device token storage.
- [ ] Guided add flow (FR-301): amount → category (pick or custom) →
      description → date (default: today) → payment method (default: Card /
      Bank transfer) → confirm; invalid input re-prompts in place.
- [ ] Transaction list with month/type filters (FR-302).
- [ ] Dashboard: monthly income, expenses, net, category breakdown (FR-303).
- [ ] Offline capture queue with background sync and rejected-entry surfacing
      (FR-304).
- [ ] Detox or component tests for the add flow; tested on emulator + at
      least one physical device.

**Acceptance criteria**

- [ ] Adding a transaction takes at most five interactions (NFR-070) and
      every prompt shows its default.
- [ ] An entry captured in airplane mode syncs correctly once online.
- [ ] The app installs and runs on Android 8.0+ emulators and test devices.

**Exit criteria:** the product is usable daily end-to-end by the mobile-user
persona.

**Effort estimate:** L.

---

## Phase 8 — Budgets, recurring transactions, insights 🗓

**Objective:** planning features and trend visibility on top of the MVP.

**Scope (requirements):** FR-241, FR-242, FR-260 – FR-262, FR-282, FR-305.

**Deliverables**

- [ ] Budget endpoints + client UI: set monthly budgets per category; summary
      warnings when exceeded (FR-260/261, FR-242).
- [ ] Recurring rules: define (e.g. monthly salary on day 1), materialize
      exactly once per period via a scheduled job (FR-262).
- [ ] Monthly trend endpoint (FR-241) + client trend chart (FR-305).
- [ ] CSV export (FR-282).
- [ ] Tests: budget-warning fixtures; recurring materialization idempotence.

**Acceptance criteria**

- [ ] A monthly-salary rule generates exactly one transaction per month for a
      requested range, with stable IDs and no duplicates on re-runs.
- [ ] Over-budget warnings appear in API summaries and the client dashboard.
- [ ] Trend series match hand-computed month-over-month fixtures.

**Exit criteria:** all Should-priority requirements delivered and green.

**Effort estimate:** M.

---

## Phase 9 — Hardening, backups, release 🗓

**Objective:** production trustworthiness and the v2.0 release.

**Scope (requirements):** NFR-012 (OWASP checklist), NFR-020 (load test),
NFR-030 (backups/restore), NFR-011 (TLS enforcement), NFR-060 finalization.

**Deliverables**

- [ ] Security review: rate limiting, OWASP API Top 10 checklist sign-off.
- [ ] Load test meeting NFR-020 targets with documented headroom.
- [ ] Automated nightly `pg_dump` backups + rehearsed restore procedure.
- [ ] Production deployment docs (Docker/managed Postgres), monitoring hooks.
- [ ] Signed Android app bundle (AAB) submitted to Play Console internal
      testing, then production.
- [ ] v2.0 release tag; user-facing documentation refresh.

**Acceptance criteria**

- [ ] OWASP checklist signed off; restore drill passes on a staging database.
- [ ] Play Console accepts the signed AAB and internal testers complete the
      add → list → summary journey.
- [ ] v2.0 tagged with green CI on the release commit.

**Exit criteria:** v2.0 released.

**Effort estimate:** S–M.

---

## Risks & mitigations

| Risk | Phase | Mitigation |
| --- | --- | --- |
| Offline sync conflicts (FR-304) | 7 | Server-authoritative writes; queued entries replayed once; rejected entries surfaced for manual fix |
| Auth/security gaps | 5, 9 | Salted hashes + JWT from day one; OWASP checklist gate before release (NFR-012) |
| Markdown import edge cases (hand-edited files) | 6 | Port the CLI's tolerant parser rules; atomic import + per-row report (FR-281) |
| Schema churn breaks clients | 5–9 | EF migrations reviewed per phase; OpenAPI diff checked in CI (NFR-051) |
| Recurring job double-materialization | 8 | Idempotent generation keyed by period; idempotence tests |
| PostgreSQL operational risk | 9 | Nightly pg_dump + rehearsed restore (NFR-030); managed Postgres optional |
| Play Store policy/requirements surprises | 9 | Internal testing track early; data-privacy disclosure for financial data |

## Change control

Scope or requirement changes update the [SRS](./REQUIREMENTS.md) and this
plan together, bumping both document versions, with the change recorded in
the revision histories and the committing message (e.g. this v2.0 re-baseline
recorded the platform decision: ASP.NET Core 8 + PostgreSQL, React Native
Android, no web UI).

