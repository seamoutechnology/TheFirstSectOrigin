-- +goose Up
-- SQL in this section is executed when the migration is applied.

ALTER TABLE player_heroes ADD COLUMN IF NOT EXISTS traits JSONB NOT NULL DEFAULT '[]';
