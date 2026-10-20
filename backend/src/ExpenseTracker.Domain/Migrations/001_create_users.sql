-- Migration 001: creates the users table.

CREATE TABLE users (
    id            uuid         PRIMARY KEY,
    email         varchar(256) NOT NULL,
    password_hash varchar(200) NOT NULL,
    currency      varchar(3)   NOT NULL DEFAULT 'USD',
    created_at    timestamptz  NOT NULL,
    updated_at    timestamptz  NOT NULL
);

CREATE UNIQUE INDEX ix_users_email ON users (email);
