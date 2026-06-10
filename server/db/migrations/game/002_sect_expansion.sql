-- Dữ liệu mở rộng cho Tông Môn (Sect) và Đệ Tử (Disciples/Heroes)

-- 1. Bảng Trait (Đặc điểm tính cách của đệ tử)
CREATE TABLE IF NOT EXISTS hero_traits (
    id SERIAL PRIMARY KEY,
    trait_code VARCHAR(50) UNIQUE NOT NULL, -- e.g., 'hardworking', 'lazy', 'alchemist_genius'
    name VARCHAR(50) NOT NULL,
    description TEXT,
    buff_type VARCHAR(50) NOT NULL, -- 'farming', 'alchemy', 'combat', 'mining'
    buff_value INT NOT NULL         -- Giá trị % buff
);

-- 2. Gán Trait cho Player Heroes (Mỗi đệ tử có thể có nhiều trait)
CREATE TABLE IF NOT EXISTS player_hero_traits (
    player_hero_id BIGINT REFERENCES player_heroes(id) ON DELETE CASCADE,
    trait_id INT REFERENCES hero_traits(id) ON DELETE CASCADE,
    PRIMARY KEY (player_hero_id, trait_id)
);

-- 3. Bảng Kỹ năng (Skills)
CREATE TABLE IF NOT EXISTS skills (
    id SERIAL PRIMARY KEY,
    skill_code VARCHAR(50) UNIQUE NOT NULL,
    name VARCHAR(50) NOT NULL,
    damage_multiplier FLOAT,
    cooldown INT,
    effect_type VARCHAR(50) -- 'heal', 'stun', 'buff'
);

-- 4. Bảng Vật phẩm (Items)
CREATE TABLE IF NOT EXISTS items (
    id SERIAL PRIMARY KEY,
    item_code VARCHAR(50) UNIQUE NOT NULL,
    name VARCHAR(50) NOT NULL,
    item_type VARCHAR(50) NOT NULL, -- 'equipment', 'consumable', 'material'
    rarity VARCHAR(10) NOT NULL,
    stats_json JSONB                -- Dữ liệu động về chỉ số của item
);

-- 5. Túi đồ người chơi (Inventory)
CREATE TABLE IF NOT EXISTS player_inventory (
    id BIGSERIAL PRIMARY KEY,
    player_id BIGINT REFERENCES players(id) ON DELETE CASCADE,
    item_id INT REFERENCES items(id),
    quantity INT NOT NULL DEFAULT 1,
    UNIQUE (player_id, item_id)
);

