-- Migration 014: Server Isolation
-- Isolate characters per server by adding server_id to players table

-- Remove the old unique constraint on user_id
ALTER TABLE players DROP CONSTRAINT IF EXISTS players_user_id_key;

-- Add server_id column, defaulting to 'zone1' for backward compatibility of existing data
ALTER TABLE players ADD COLUMN IF NOT EXISTS server_id VARCHAR(32) NOT NULL DEFAULT 'zone1';

-- Add unique constraint for (user_id, server_id) to allow one character per account per server
ALTER TABLE players ADD CONSTRAINT players_user_id_server_id_key UNIQUE (user_id, server_id);

-- Add index on server_id for faster lookups
CREATE INDEX IF NOT EXISTS idx_players_server_id ON players(server_id);
