CREATE TABLE IF NOT EXISTS `transactions` (
    id SERIAL PRIMARY KEY,
    gateway varchar(255) NOT NULL,
    amount INT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
)