-- +goose Up
-- SQL in this section is executed when the migration is applied.

CREATE TABLE IF NOT EXISTS player_vip (
    player_id BIGINT PRIMARY KEY REFERENCES players(id) ON DELETE CASCADE,
    vip_level INT NOT NULL DEFAULT 0,
    expire_at TIMESTAMP WITH TIME ZONE,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS player_unlocked_functions (
    player_id BIGINT REFERENCES players(id) ON DELETE CASCADE,
    function_code VARCHAR(100) NOT NULL,
    unlocked_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (player_id, function_code)
);
