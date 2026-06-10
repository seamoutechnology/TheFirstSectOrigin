-- +goose Up
-- SQL in this section is executed when the migration is applied.

CREATE TABLE IF NOT EXISTS trait_configs (
    trait_code VARCHAR(50) PRIMARY KEY,
    weight INT NOT NULL DEFAULT 100,
    json_data TEXT NOT NULL,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- +goose Down
-- SQL in this section is executed when the migration is rolled back.

DROP TABLE IF EXISTS trait_configs;
