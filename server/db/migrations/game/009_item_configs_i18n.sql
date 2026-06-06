-- +goose Up
-- SQL in this section is executed when the migration is applied.

ALTER TABLE item_configs RENAME COLUMN name TO name_key;
ALTER TABLE item_configs RENAME COLUMN description TO desc_key;
ALTER TABLE item_configs ADD COLUMN effects JSONB DEFAULT '[]';

