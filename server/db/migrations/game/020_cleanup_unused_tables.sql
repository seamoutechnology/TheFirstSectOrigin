-- +goose Up
-- SQL in this section is executed when the migration is applied.

DROP TABLE IF EXISTS items CASCADE;
DROP TABLE IF EXISTS player_inventory CASCADE;
DROP TABLE IF EXISTS item_instances CASCADE;
DROP TABLE IF EXISTS alliances CASCADE;
DROP TABLE IF EXISTS crafting_recipes CASCADE;
DROP TABLE IF EXISTS hero_traits CASCADE;
DROP TABLE IF EXISTS player_hero_traits CASCADE;
DROP TABLE IF EXISTS player_daily_rewards CASCADE;

-- +goose Down
-- SQL in this section is executed when the migration is rolled back.
