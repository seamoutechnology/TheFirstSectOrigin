-- +goose Up
-- SQL in this section is executed when the migration is applied.

ALTER TABLE players ADD COLUMN IF NOT EXISTS power BIGINT NOT NULL DEFAULT 0;

CREATE TABLE IF NOT EXISTS player_stages (
    player_id BIGINT NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    stage_id VARCHAR(50) NOT NULL,
    completed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (player_id, stage_id)
);

CREATE INDEX IF NOT EXISTS idx_players_power ON players(power DESC);
CREATE INDEX IF NOT EXISTS idx_player_stages_player_id ON player_stages(player_id);

-- +goose Down
-- SQL in this section is executed when the migration is rolled back.

DROP TABLE IF EXISTS player_stages;
ALTER TABLE players DROP COLUMN IF EXISTS power;
