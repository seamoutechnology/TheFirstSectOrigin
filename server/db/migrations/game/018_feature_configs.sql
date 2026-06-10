-- +goose Up
-- SQL in this section is executed when the migration is applied.

CREATE TABLE IF NOT EXISTS feature_configs (
    feature_code VARCHAR(100) PRIMARY KEY,
    name_key VARCHAR(255) NOT NULL,
    icon VARCHAR(255) NOT NULL,
    required_player_level INT NOT NULL DEFAULT 0,
    required_mission_code VARCHAR(100) NOT NULL DEFAULT '',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);
