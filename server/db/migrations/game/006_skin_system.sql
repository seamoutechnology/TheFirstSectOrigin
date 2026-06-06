-- Hệ thống Skin/Trang phục
CREATE TABLE IF NOT EXISTS skins (
    id SERIAL PRIMARY KEY,
    skin_code VARCHAR(100) NOT NULL UNIQUE, -- ID mẫu skin (ví dụ: 'skin_luc_tuyet_ky_wedding')
    owner_type VARCHAR(20) NOT NULL,        -- 'sect_leader' hoặc 'disciple'
    target_code VARCHAR(100) NOT NULL,      -- ID nhân vật có thể mặc (ví dụ: 'luc_tuyet_ky' hoặc 'main_player')
    name VARCHAR(255) NOT NULL,
    rarity VARCHAR(20) DEFAULT 'Common',
    stats_bonus JSONB,                       -- Chỉ số cộng thêm nếu có
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Quan hệ sở hữu skin của người chơi
CREATE TABLE IF NOT EXISTS player_skins (
    player_id BIGINT REFERENCES players(id) ON DELETE CASCADE,
    skin_code VARCHAR(100) REFERENCES skins(skin_code),
    is_unlocked BOOLEAN DEFAULT TRUE,
    unlocked_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (player_id, skin_code)
);
