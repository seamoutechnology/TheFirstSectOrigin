-- +goose Up
-- SQL in this section is executed when the migration is applied.

CREATE TABLE IF NOT EXISTS effect_configs (
    effect_code VARCHAR(100) PRIMARY KEY,
    name_key VARCHAR(255) NOT NULL,
    desc_key TEXT,
    effect_type VARCHAR(50) NOT NULL,
    value_type VARCHAR(50) NOT NULL, -- 'flat' or 'percent'
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

