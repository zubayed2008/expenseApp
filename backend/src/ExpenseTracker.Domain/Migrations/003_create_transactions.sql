-- Migration 003: creates the transactions table.
-- Money is numeric(18,2) and strictly positive (C-2, FR-221); the sign of a
-- balance change derives from "type" (expense/income).

CREATE TABLE transactions (
    id             uuid          PRIMARY KEY,
    user_id        uuid          NOT NULL,
    type           varchar(10)   NOT NULL,
    date           date          NOT NULL,
    amount         numeric(18,2) NOT NULL,
    category_id    uuid          NOT NULL,
    description    varchar(500)  NOT NULL,
    payment_method varchar(100)  NOT NULL,
    created_at     timestamptz   NOT NULL,
    updated_at     timestamptz   NOT NULL,
    CONSTRAINT ck_transactions_amount_positive CHECK (amount > 0),
    CONSTRAINT fk_transactions_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE,
    CONSTRAINT fk_transactions_category FOREIGN KEY (category_id) REFERENCES categories (id)
);

-- Common query paths: per-account listings and month/type filters (FR-226).
CREATE INDEX ix_transactions_user_id_date ON transactions (user_id, date);
CREATE INDEX ix_transactions_user_id_type ON transactions (user_id, type);
CREATE INDEX ix_transactions_category_id ON transactions (category_id);
