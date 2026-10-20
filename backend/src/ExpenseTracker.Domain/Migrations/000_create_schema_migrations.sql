-- Migration 000: creates the migration tracking table.
-- Applied scripts are recorded here so you can always see where you are:
--   SELECT * FROM schema_migrations ORDER BY version;
-- Idempotent: safe to run more than once (manual or automated runner).

CREATE TABLE IF NOT EXISTS schema_migrations (
    version     integer PRIMARY KEY,
    name        text    NOT NULL,
    applied_at  timestamptz NOT NULL DEFAULT now()
);
