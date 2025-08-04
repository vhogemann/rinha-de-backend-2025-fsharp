CREATE TABLE IF NOT EXISTS transactions (
    id SERIAL PRIMARY KEY,
    correlation_id UUID NOT NULL UNIQUE,
    gateway TEXT NOT NULL,
    amount INTEGER NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_gateway ON transactions (gateway);
CREATE INDEX idx_created_at ON transactions (created_at);