-- +goose Up
-- SQL in this section is executed when the migration is applied.
ALTER TABLE effect_configs ADD COLUMN icon VARCHAR(255) NOT NULL DEFAULT '';
