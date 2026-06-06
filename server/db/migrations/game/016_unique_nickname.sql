-- Migration 016: Unique Nickname per Server
-- Enforce unique character nicknames (case-insensitive) per server/zone

CREATE UNIQUE INDEX IF NOT EXISTS idx_players_unique_nickname_server 
ON players (LOWER(nickname), server_id);
