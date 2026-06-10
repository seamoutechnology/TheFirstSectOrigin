-- +goose Up
-- SQL in this section is executed when the migration is applied.

CREATE TABLE IF NOT EXISTS gift_codes (
    code VARCHAR(100) PRIMARY KEY,
    reward_gold BIGINT NOT NULL DEFAULT 0,
    reward_diamond BIGINT NOT NULL DEFAULT 0,
    reward_items JSONB NOT NULL DEFAULT '[]', -- e.g. [{"item_code": "00000", "quantity": 100}]
    max_uses INT NOT NULL DEFAULT 1,
    used_count INT NOT NULL DEFAULT 0,
    expires_at TIMESTAMP WITH TIME ZONE
);

CREATE TABLE IF NOT EXISTS gift_code_usages (
    player_id BIGINT NOT NULL,
    code VARCHAR(100) NOT NULL REFERENCES gift_codes(code) ON DELETE CASCADE,
    used_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (player_id, code)
);

CREATE TABLE IF NOT EXISTS recharge_history (
    id SERIAL PRIMARY KEY,
    player_id BIGINT NOT NULL,
    amount BIGINT NOT NULL,
    diamond_reward BIGINT NOT NULL,
    gold_reward BIGINT NOT NULL,
    status VARCHAR(50) NOT NULL DEFAULT 'success',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- Seed gift codes
INSERT INTO gift_codes (code, reward_gold, reward_diamond, reward_items, max_uses, expires_at) VALUES
('SECT666', 10000, 100, '[{"item_code": "00000", "quantity": 100}]', 9999, '2030-12-31 23:59:59+00'),
('SECT999', 50000, 500, '[{"item_code": "00002", "quantity": 100}, {"item_code": "00003", "quantity": 100}]', 9999, '2030-12-31 23:59:59+00')
ON CONFLICT (code) DO NOTHING;
