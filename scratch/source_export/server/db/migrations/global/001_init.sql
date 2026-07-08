-- +goose Up
-- SQL in this section is executed when the migration is applied.

-- ============================================================
-- Users (Tài khoản người chơi)
-- ============================================================
CREATE TABLE IF NOT EXISTS users (
    id          BIGSERIAL PRIMARY KEY,
    username    VARCHAR(32)  NOT NULL UNIQUE,
    email       VARCHAR(128) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    is_banned   BOOLEAN      NOT NULL DEFAULT FALSE,
    ban_reason  TEXT,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- Bảng liên kết tài khoản mạng xã hội (Google, Apple, Facebook)
CREATE TABLE IF NOT EXISTS social_accounts (
    id          BIGSERIAL PRIMARY KEY,
    user_id     BIGINT       NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    provider    VARCHAR(32)  NOT NULL,  -- google | apple | facebook
    provider_id VARCHAR(255) NOT NULL,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    UNIQUE (provider, provider_id)
);

-- ============================================================
-- Zones / Server con (Có bao gồm cột cho CPU, RAM, players)
-- ============================================================
CREATE TABLE IF NOT EXISTS zones (
    id          SERIAL PRIMARY KEY,
    name        VARCHAR(64)  NOT NULL,
    status      VARCHAR(16)  NOT NULL DEFAULT 'normal', -- normal | crowded | maintenance
    gateway_url VARCHAR(255) NOT NULL,
    is_active   BOOLEAN      NOT NULL DEFAULT TRUE,
    cpu_usage   NUMERIC(5,2) DEFAULT 0.0,
    ram_usage   NUMERIC(5,2) DEFAULT 0.0,
    player_count INT         DEFAULT 0,
    max_players INT          DEFAULT 1000,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- Seed server mặc định
INSERT INTO zones (name, status, gateway_url, max_players) VALUES
('Thanh Long 1', 'normal', 'localhost:50052', 1000)
ON CONFLICT DO NOTHING;

-- ============================================================
-- Announcements (Thông báo hệ thống)
-- ============================================================
CREATE TABLE IF NOT EXISTS announcements (
    id SERIAL PRIMARY KEY,
    type VARCHAR(50) NOT NULL, -- MAINTENANCE, RULES, ACTIVITY, NEWS
    title TEXT NOT NULL,
    content TEXT NOT NULL,
    start_at TIMESTAMP NOT NULL,
    end_at TIMESTAMP NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================
-- Indexes
-- ============================================================
CREATE INDEX IF NOT EXISTS idx_users_email    ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
CREATE INDEX IF NOT EXISTS idx_announcements_active ON announcements(is_active, start_at, end_at);

-- +goose Down
-- SQL in this section is executed when the migration is rolled back.
