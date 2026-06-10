-- +goose Up
-- SQL in this section is executed when the migration is applied.

-- 1. Rename column user_id to player_id in user_items
ALTER TABLE user_items RENAME COLUMN user_id TO player_id;

-- 2. Drop old index
DROP INDEX IF EXISTS idx_user_items_user_id;

-- 3. Create new index on player_id
CREATE INDEX idx_user_items_player_id ON user_items(player_id);

-- 4. Add foreign key referencing players(id)
-- Note: In a production DB with pre-existing data, this might fail if some player_ids do not exist.
-- But since it's development, we can apply Cascade delete so deleting a player cleans up their items.
ALTER TABLE user_items ADD CONSTRAINT fk_user_items_player FOREIGN KEY (player_id) REFERENCES players(id) ON DELETE CASCADE;

-- +goose Down
-- SQL in this section is executed when the migration is rolled back.
ALTER TABLE user_items DROP CONSTRAINT IF EXISTS fk_user_items_player;
DROP INDEX IF EXISTS idx_user_items_player_id;
ALTER TABLE user_items RENAME COLUMN player_id TO user_id;
CREATE INDEX idx_user_items_user_id ON user_items(user_id);
