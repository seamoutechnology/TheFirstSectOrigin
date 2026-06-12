-- Migration 001: Game DB Schema
-- Chạy một lần khi setup zone server lần đầu

-- ============================================================
-- Players (một player tương ứng 1 user_id trên mỗi zone)
-- ============================================================
CREATE TABLE IF NOT EXISTS players (
    id              BIGSERIAL PRIMARY KEY,
    user_id         BIGINT       NOT NULL UNIQUE, -- ID từ Global DB
    nickname        VARCHAR(32)  NOT NULL,
    level           INT          NOT NULL DEFAULT 1,
    exp             BIGINT       NOT NULL DEFAULT 0,
    gold            BIGINT       NOT NULL DEFAULT 1000,
    diamond         BIGINT       NOT NULL DEFAULT 100,
    stamina         INT          NOT NULL DEFAULT 100,
    max_stamina     INT          NOT NULL DEFAULT 100,
    last_stamina_at TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- ============================================================
-- Buildings definition (game config - ít thay đổi)
-- ============================================================
CREATE TABLE IF NOT EXISTS buildings (
    code        VARCHAR(32)  PRIMARY KEY,
    name        VARCHAR(64)  NOT NULL,
    max_level   INT          NOT NULL DEFAULT 20,
    description TEXT
);

-- Seed dữ liệu công trình
INSERT INTO buildings (code, name, max_level, description) VALUES
('dorm', 'dorm', 1, 'dorm_desc'),
('farm', 'farm', 5, 'farm_desc'),
('main_hall', 'main_hall', 5, 'main_hall_desc'),
('summon_hall', 'summon_hall', 5, 'summon_hall_desc')
ON CONFLICT DO NOTHING;

-- ============================================================
-- Player buildings (trạng thái công trình của từng player)
-- ============================================================
CREATE TABLE IF NOT EXISTS player_buildings (
    id               BIGSERIAL    PRIMARY KEY,
    player_id        BIGINT       NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    building_code    VARCHAR(32)  NOT NULL REFERENCES buildings(code),
    level            INT          NOT NULL DEFAULT 1,
    upgrade_end_at   TIMESTAMPTZ,              -- NULL = không nâng cấp
    last_collect_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    created_at       TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- ============================================================
-- Hero Templates (game data - không thay đổi thường xuyên)
-- ============================================================
CREATE TABLE IF NOT EXISTS hero_templates (
    code          VARCHAR(32)  PRIMARY KEY,
    name          VARCHAR(64)  NOT NULL,
    rarity        VARCHAR(8)   NOT NULL CHECK (rarity IN ('R','SR','SSR','UR')),
    element       VARCHAR(16)  NOT NULL CHECK (element IN ('fire','water','wood','light','dark')),
    role          VARCHAR(16)  NOT NULL CHECK (role IN ('tank','warrior','mage','assassin','healer')),
    base_hp       INT          NOT NULL DEFAULT 1000,
    base_atk      INT          NOT NULL DEFAULT 100,
    base_def      INT          NOT NULL DEFAULT 80,
    base_speed    INT          NOT NULL DEFAULT 100,
    gacha_weight  INT          NOT NULL DEFAULT 100, -- trọng số rơi trong gacha
    is_active     BOOLEAN      NOT NULL DEFAULT TRUE
);

-- Seed một số tướng mẫu
INSERT INTO hero_templates (code, name, rarity, element, role, base_hp, base_atk, base_def, base_speed, gacha_weight) VALUES
ON CONFLICT DO NOTHING;

-- ============================================================
-- Gacha Banners
-- ============================================================
CREATE TABLE IF NOT EXISTS gacha_banners (
    id           SERIAL       PRIMARY KEY,
    name         VARCHAR(64)  NOT NULL,
    description  TEXT,
    cost_diamond INT          NOT NULL DEFAULT 300,
    pity_count   INT          NOT NULL DEFAULT 80, -- đảm bảo SSR sau N lần
    is_active    BOOLEAN      NOT NULL DEFAULT TRUE,
    end_at       TIMESTAMPTZ
);

-- Seed banner mặc định
INSERT INTO gacha_banners (name, description, cost_diamond, pity_count, is_active) VALUES
ON CONFLICT DO NOTHING;

-- ============================================================
-- Player Heroes (tướng đang sở hữu)
-- ============================================================
CREATE TABLE IF NOT EXISTS player_heroes (
    id          BIGSERIAL    PRIMARY KEY,
    player_id   BIGINT       NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    hero_code   VARCHAR(32)  NOT NULL REFERENCES hero_templates(code),
    level       INT          NOT NULL DEFAULT 1,
    star        INT          NOT NULL DEFAULT 1,
    exp         BIGINT       NOT NULL DEFAULT 0,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- ============================================================
-- Player Formation (đội hình đã set)
-- ============================================================
CREATE TABLE IF NOT EXISTS player_formations (
    player_id       BIGINT   NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    position        INT      NOT NULL CHECK (position BETWEEN 0 AND 8),
    player_hero_id  BIGINT   REFERENCES player_heroes(id) ON DELETE SET NULL,
    PRIMARY KEY (player_id, position)
);

-- ============================================================
-- Gacha Pity tracker (theo dõi pity của player cho từng banner)
-- ============================================================
CREATE TABLE IF NOT EXISTS player_gacha_pity (
    player_id   BIGINT  NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    banner_id   INT     NOT NULL REFERENCES gacha_banners(id),
    pull_count  INT     NOT NULL DEFAULT 0,
    PRIMARY KEY (player_id, banner_id)
);

-- ============================================================
-- Indexes
-- ============================================================
CREATE INDEX IF NOT EXISTS idx_player_heroes_player  ON player_heroes(player_id);
CREATE INDEX IF NOT EXISTS idx_player_buildings_player ON player_buildings(player_id);
CREATE INDEX IF NOT EXISTS idx_players_user_id       ON players(user_id);
