-- +goose Up
-- SQL in this section is executed when the migration is applied.

ALTER TABLE item_configs ADD COLUMN required_level INT NOT NULL DEFAULT 1;
