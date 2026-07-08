-- +goose Up
-- SQL in this section is executed when the migration is applied.

-- ============================================================
-- Players (một player tương ứng 1 user_id trên mỗi zone)
-- ============================================================
CREATE TABLE IF NOT EXISTS players (
    id              BIGSERIAL PRIMARY KEY,
    user_id         BIGINT       NOT NULL, -- ID từ Global DB
    nickname        VARCHAR(32)  NOT NULL,
    level           INT          NOT NULL DEFAULT 1,
    exp             BIGINT       NOT NULL DEFAULT 0,
    gold            BIGINT       NOT NULL DEFAULT 1000,
    diamond         BIGINT       NOT NULL DEFAULT 100,
    stamina         INT          NOT NULL DEFAULT 100,
    max_stamina     INT          NOT NULL DEFAULT 100,
    last_stamina_at TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    power           BIGINT       NOT NULL DEFAULT 0,
    server_id       VARCHAR(32)  NOT NULL DEFAULT 'zone1',
    created_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
    CONSTRAINT players_user_id_server_id_key UNIQUE (user_id, server_id)
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

INSERT INTO buildings (code, name, max_level, description) VALUES
('dorm', 'dorm', 1, 'dorm_desc'),
('farm', 'farm', 5, 'farm_desc'),
('main_hall', 'main_hall', 5, 'main_hall_desc'),
('summon_hall', 'summon_hall', 5, 'summon_hall_desc')
ON CONFLICT (code) DO NOTHING;

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

INSERT INTO hero_templates (code, name, rarity, element, role, base_hp, base_atk, base_def, base_speed, gacha_weight) VALUES
('FIRE_WARRIOR_01',  'Lửa Chiến Binh',   'R',   'fire',  'warrior',  1200, 120, 80,  105, 500),
('WATER_TANK_01',    'Thủy Hộ Thuẫn',    'R',   'water', 'tank',     2000, 80,  150, 90,  500),
('WOOD_HEALER_01',   'Mộc Thánh Nữ',     'SR',  'wood',  'healer',   1500, 90,  100, 110, 150),
('LIGHT_MAGE_01',    'Thánh Pháp Sư',    'SR',  'light', 'mage',     1100, 180, 60,  115, 150),
('DARK_ASSASSIN_01', 'Hắc Sát Thần',     'SSR', 'dark',  'assassin', 1000, 250, 50,  140, 40),
('FIRE_GENERAL_01',  'Hỏa Thần Tướng',   'SSR', 'fire',  'warrior',  1800, 220, 120, 125, 40),
('LIGHT_DEITY_01',   'Thần Ánh Sáng',    'UR',  'light', 'mage',     2000, 350, 100, 150, 10)
ON CONFLICT (code) DO NOTHING;

-- ============================================================
-- Gacha Banners
-- ============================================================
CREATE TABLE IF NOT EXISTS gacha_banners (
    id           SERIAL       PRIMARY KEY,
    name         VARCHAR(64)  NOT NULL,
    description  TEXT,
    cost_diamond INT          NOT NULL DEFAULT 300,
    cost_item    VARCHAR(50)  DEFAULT '',
    cost_gold    INT          DEFAULT 0,
    pity_count   INT          NOT NULL DEFAULT 80, -- đảm bảo SSR sau N lần
    is_active    BOOLEAN      NOT NULL DEFAULT TRUE,
    end_at       TIMESTAMPTZ
);

INSERT INTO gacha_banners (name, description, cost_diamond, pity_count, is_active, cost_item, cost_gold) VALUES
('Triệu Hồi Cơ Bản', 'Banner thường xuyên, tỷ lệ SSR 1.5%', 300, 80, TRUE, '', 0),
('Vòng Quay Linh Thảo', 'Sử dụng Linh Thảo để chiêu mộ đệ tử xuất chúng!', 0, 80, true, '00003', 0),
('Chiêu Mộ Bằng Vàng', 'Sử dụng Vàng tích luỹ để tìm kiếm đồng hành mới!', 0, 80, true, '', 5000)
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
    traits      JSONB        NOT NULL DEFAULT '[]'::jsonb,
    skills      JSONB        NOT NULL DEFAULT '[]'::jsonb,
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
-- Skills (kỹ năng của đệ tử)
-- ============================================================
CREATE TABLE IF NOT EXISTS skills (
    id SERIAL PRIMARY KEY,
    skill_code VARCHAR(50) UNIQUE NOT NULL,
    name VARCHAR(50) NOT NULL,
    damage_multiplier FLOAT,
    cooldown INT,
    effect_type VARCHAR(50)
);

INSERT INTO skills (skill_code, name, damage_multiplier, cooldown, effect_type) VALUES
('skill_shadow_strike', 'Ám Ảnh Kích', 1.8, 2, 'damage'),
('skill_slash', 'Trảm Kích', 1.2, 0, 'damage'),
('skill_blood_claw', 'Huyết Trảo', 1.5, 3, 'damage'),
('skill_fireball', 'Hỏa Cầu Thuật', 1.4, 1, 'damage'),
('skill_shield', 'Hộ Thể Kim Cang', 0.0, 3, 'buff'),
('skill_meteor', 'Thiên Thạch Trụy', 2.2, 4, 'damage'),
('skill_blade_tempest', 'Phong Nhận Kiếm Trận', 1.6, 3, 'damage'),
('skill_heal', 'Hồi Xuân Thuật', 1.5, 3, 'heal'),
('skill_divine_light', 'Thần Thánh Chi Quang', 2.0, 4, 'damage'),
('skill_lightning_storm', 'Lôi Vân Bạo Vũ', 1.8, 3, 'damage'),
('skill_frozen_armor', 'Băng Giáp', 0.0, 3, 'buff'),
('skill_holy_revive', 'Phục Sinh Thuật', 2.0, 5, 'heal')
ON CONFLICT (skill_code) DO NOTHING;

-- ============================================================
-- Inventory (Túi đồ người chơi)
-- ============================================================
CREATE TABLE IF NOT EXISTS user_items (
    id BIGSERIAL PRIMARY KEY,
    player_id BIGINT NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    item_code VARCHAR(100) NOT NULL,
    quantity INT NOT NULL DEFAULT 1,
    stats JSONB DEFAULT '{}',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================
-- Item Configs
-- ============================================================
CREATE TABLE IF NOT EXISTS item_configs (
    item_code VARCHAR(100) PRIMARY KEY,
    name_key VARCHAR(255) NOT NULL,
    type VARCHAR(50) NOT NULL,
    rarity VARCHAR(50) NOT NULL,
    icon VARCHAR(255) NOT NULL,
    desc_key TEXT,
    max_stack INT NOT NULL DEFAULT 1,
    required_level INT NOT NULL DEFAULT 1,
    sources JSONB DEFAULT '[]',
    effects JSONB DEFAULT '[]',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO item_configs (item_code, name_key, type, rarity, icon, desc_key, max_stack, required_level, effects) VALUES
('recruit_ticket', 'recruit_ticket', 'CONSUMABLE', 'RARE', 'recruit_ticket_icon', 'recruit_ticket_des', 99, 1, '[]'::jsonb),
('shop_reset_ticket', 'shop_reset_ticket', 'CONSUMABLE', 'RARE', 'shop_reset_ticket_icon', 'shop_reset_ticket_des', 99, 1, '[]'::jsonb),
('iron_ore', 'iron_ore', 'CONSUMABLE', 'UNCOMMON', 'iron_ore_icon', 'iron_ore_des', 999, 1, '[]'::jsonb),
('herb_spirit', 'herb_spirit', 'CONSUMABLE', 'UNCOMMON', 'herb_spirit_icon', 'herb_spirit_des', 999, 1, '[]'::jsonb),
('break_pill', 'break_pill', 'CONSUMABLE', 'EPIC', 'break_pill_icon', 'break_pill_des', 99, 1, '[]'::jsonb),
('stamina_potion', 'stamina_potion', 'CONSUMABLE', 'RARE', 'stamina_potion_icon', 'stamina_potion_des', 99, 1, '[{"effect_code": "EFF_ADD_STAMINA", "value": 50}]'::jsonb),
('speed_hourglass', 'speed_hourglass', 'CONSUMABLE', 'RARE', 'speed_hourglass_icon', 'speed_hourglass_des', 99, 1, '[]'::jsonb),
('guild_coin', 'guild_coin', 'CURRENCY', 'RARE', 'guild_coin_icon', 'guild_coin_des', 9999, 1, '[]'::jsonb),
('artifact_shard', 'artifact_shard', 'CONSUMABLE', 'LEGENDARY', 'artifact_shard_icon', 'artifact_shard_des', 99, 1, '[]'::jsonb),
('EXP_pill', 'EXP_pill', 'CONSUMABLE', 'RARE', 'EXP_pill_icon', 'EXP_pill_des', 99, 1, '[{"effect_code": "EFF_ADD_EXP", "value": 100}]'::jsonb),
('00001', 'gold', 'CURRENCY', 'COMMON', 'gold_icon', 'gold_des', 999999, 1, '[]'::jsonb),
('00000', 'coin', 'CURRENCY', 'COMMON', 'coin_icon', 'coin_des', 999999, 1, '[]'::jsonb),
('00002', 'stone_1', 'CONSUMABLE', 'COMMON', 'stone_1_icon', 'stone_1_des', 999999, 1, '[]'::jsonb),
('00003', 'wood_1', 'CONSUMABLE', 'COMMON', 'wood_1_icon', 'wood_1_des', 999999, 1, '[]'::jsonb),
('stamina', 'stamina', 'CURRENCY', 'COMMON', 'stamina_icon', 'stamina_des', 999999, 1, '[]'::jsonb)
ON CONFLICT (item_code) DO UPDATE 
SET name_key = EXCLUDED.name_key,
    type = EXCLUDED.type,
    rarity = EXCLUDED.rarity,
    icon = EXCLUDED.icon,
    desc_key = EXCLUDED.desc_key,
    required_level = EXCLUDED.required_level,
    effects = EXCLUDED.effects;

-- ============================================================
-- Effect Configs
-- ============================================================
CREATE TABLE IF NOT EXISTS effect_configs (
    effect_code VARCHAR(100) PRIMARY KEY,
    name_key VARCHAR(255) NOT NULL,
    desc_key TEXT,
    effect_type VARCHAR(50) NOT NULL,
    value_type VARCHAR(50) NOT NULL, -- 'flat' or 'percent'
    min_value FLOAT NOT NULL DEFAULT 0,
    max_value FLOAT NOT NULL DEFAULT 100,
    icon VARCHAR(255) NOT NULL DEFAULT '',
    created_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================
-- Stage Configs
-- ============================================================
CREATE TABLE IF NOT EXISTS stage_configs (
    stage_id VARCHAR(50) PRIMARY KEY,
    json_data TEXT NOT NULL,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================
-- Admin Maps
-- ============================================================
CREATE TABLE IF NOT EXISTS admin_maps (
    id VARCHAR(50) PRIMARY KEY,
    json_data TEXT NOT NULL,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

INSERT INTO admin_maps (id, json_data) VALUES
('default_base', $$﻿{     "gridWidth": 32,     "gridHeight": 32,     "fogData": [         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0     ],     "items": [],     "buildings": [         {             "instance_id": 0,             "id": "farm",             "x": 32,             "y": 27,             "flipX": false,             "level": 1,             "state": 0,             "currentHP": 1000.0         },         {             "instance_id": 0,             "id": "main_hall",             "x": 23,             "y": 26,             "flipX": false,             "level": 1,             "state": 0,             "currentHP": 1000.0         }     ],     "terrainData": [         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0,         0     ],     "groundTiles": [],     "groundLayers": [] }$$)
ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- Player Maps
-- ============================================================
CREATE TABLE IF NOT EXISTS player_maps (
    player_id BIGINT PRIMARY KEY REFERENCES players(id) ON DELETE CASCADE,
    json_data TEXT NOT NULL,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);


-- ============================================================
-- Trait Configs
-- ============================================================
CREATE TABLE IF NOT EXISTS trait_configs (
    trait_code VARCHAR(50) PRIMARY KEY,
    weight INT NOT NULL DEFAULT 100,
    json_data TEXT NOT NULL,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ============================================================
-- Player Stages (tiến độ ải của người chơi)
-- ============================================================
CREATE TABLE IF NOT EXISTS player_stages (
    player_id BIGINT NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    stage_id VARCHAR(50) NOT NULL,
    completed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    PRIMARY KEY (player_id, stage_id)
);

-- ============================================================
-- Shop Configs & Player Shops
-- ============================================================
CREATE TABLE IF NOT EXISTS shop_items (
    id              SERIAL       PRIMARY KEY,
    shop_item_id    VARCHAR(50)  NOT NULL UNIQUE,
    shop_type       VARCHAR(50)  NOT NULL,
    item_code       VARCHAR(50)  NOT NULL,
    amount          INT          NOT NULL DEFAULT 1,
    original_price  JSONB        NOT NULL DEFAULT '[]',
    is_discountable BOOLEAN      NOT NULL DEFAULT TRUE
);

INSERT INTO shop_items (shop_item_id, shop_type, item_code, amount, original_price, is_discountable) VALUES
('daily_recruit_ticket', 'daily', 'recruit_ticket', 1, '[{"item_code": "00000", "amount": 100}]', true),
('daily_stamina_potion', 'daily', 'stamina_potion', 1, '[{"item_code": "00000", "amount": 50}]', true),
('daily_speed_hourglass', 'daily', 'speed_hourglass', 1, '[{"item_code": "00001", "amount": 30}]', true),
('daily_EXP_pill', 'daily', 'EXP_pill', 5, '[{"item_code": "iron_ore", "amount": 10}]', true),
('guild_recruit_ticket', 'guild', 'recruit_ticket', 1, '[{"item_code": "guild_coin", "amount": 200}]', false),
('guild_artifact_shard', 'guild', 'artifact_shard', 2, '[{"item_code": "guild_coin", "amount": 500}]', false)
ON CONFLICT (shop_item_id) DO NOTHING;

CREATE TABLE IF NOT EXISTS player_shops (
    id              SERIAL       PRIMARY KEY,
    player_id       INT          NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    shop_type       VARCHAR(50)  NOT NULL,
    shop_item_id    VARCHAR(50)  NOT NULL,
    item_code       VARCHAR(50)  NOT NULL,
    amount          INT          NOT NULL,
    final_price     JSONB        NOT NULL,
    discount_pct    INT          NOT NULL DEFAULT 0,
    is_bought       BOOLEAN      NOT NULL DEFAULT FALSE,
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS player_shop_resets (
    player_id       INT          NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    shop_type       VARCHAR(50)  NOT NULL,
    next_refresh_at TIMESTAMPTZ  NOT NULL,
    PRIMARY KEY (player_id, shop_type)
);

-- ============================================================
-- Indexes
-- ============================================================
CREATE INDEX IF NOT EXISTS idx_player_heroes_player  ON player_heroes(player_id);
CREATE INDEX IF NOT EXISTS idx_player_buildings_player ON player_buildings(player_id);
CREATE INDEX IF NOT EXISTS idx_players_user_id       ON players(user_id);
CREATE INDEX IF NOT EXISTS idx_user_items_player_id ON user_items(player_id);
CREATE INDEX IF NOT EXISTS idx_user_items_item_code ON user_items(item_code);
CREATE INDEX IF NOT EXISTS idx_players_power ON players(power DESC);
CREATE INDEX IF NOT EXISTS idx_player_stages_player_id ON player_stages(player_id);

-- +goose Down
-- SQL in this section is executed when the migration is rolled back.

