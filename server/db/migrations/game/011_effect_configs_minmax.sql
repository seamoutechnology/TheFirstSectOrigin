-- +goose Up
-- SQL in this section is executed when the migration is applied.
ALTER TABLE effect_configs ADD COLUMN min_value FLOAT NOT NULL DEFAULT 0;
ALTER TABLE effect_configs ADD COLUMN max_value FLOAT NOT NULL DEFAULT 100;

