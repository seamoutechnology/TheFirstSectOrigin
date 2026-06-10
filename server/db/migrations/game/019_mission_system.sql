-- +goose Up
-- SQL in this section is executed when the migration is applied.

CREATE TABLE IF NOT EXISTS mission_templates (
    mission_id SERIAL PRIMARY KEY,
    title VARCHAR(255) NOT NULL,
    description VARCHAR(255) NOT NULL,
    type INT NOT NULL DEFAULT 1, -- 0: DAILY, 1: MAIN, 2: SIDE, 3: SECT
    target_type VARCHAR(100) NOT NULL, -- "player_level", "build_upgrade", "craft_item"
    target_param VARCHAR(100) NOT NULL DEFAULT '',
    target_progress INT NOT NULL DEFAULT 1,
    rewards JSONB NOT NULL DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS player_missions (
    player_id BIGINT REFERENCES players(id) ON DELETE CASCADE,
    mission_id INT REFERENCES mission_templates(mission_id) ON DELETE CASCADE,
    status INT NOT NULL DEFAULT 1, -- 0: LOCKED, 1: AVAILABLE, 2: IN_PROGRESS, 3: COMPLETED, 4: REWARDED
    current_progress INT NOT NULL DEFAULT 0,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (player_id, mission_id)
);

