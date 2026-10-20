-- Migration 002: creates the categories table.
-- Default categories are seeded per account at registration (FR-223).

CREATE TABLE categories (
    id         uuid         PRIMARY KEY,
    user_id    uuid         NOT NULL,
    name       varchar(100) NOT NULL,
    kind       varchar(10)  NOT NULL,
    created_at timestamptz  NOT NULL,
    CONSTRAINT fk_categories_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
);

-- One category name per kind per account.
CREATE UNIQUE INDEX ix_categories_user_id_kind_name ON categories (user_id, kind, name);
