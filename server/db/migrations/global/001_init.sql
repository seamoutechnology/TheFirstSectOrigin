-- Migration 001: Khởi tạo schema cho Global DB (Auth & Account)
-- Chạy một lần khi setup lần đầu

-- Bảng người dùng chính
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

-- Bảng danh sách Zone / Server con
CREATE TABLE IF NOT EXISTS zones (
    id          SERIAL PRIMARY KEY,
    name        VARCHAR(64)  NOT NULL,
    status      VARCHAR(16)  NOT NULL DEFAULT 'normal', -- normal | crowded | maintenance
    gateway_url VARCHAR(255) NOT NULL,
    is_active   BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- Seed dữ liệu zone mặc định
INSERT INTO zones (name, status, gateway_url) VALUES
    ('Server 1 - Sơ Kỳ', 'normal', 'localhost:50052')
ON CONFLICT DO NOTHING;

-- Index
CREATE INDEX IF NOT EXISTS idx_users_email    ON users(email);
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);
