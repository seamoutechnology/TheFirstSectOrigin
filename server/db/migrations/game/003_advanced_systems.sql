-- Hệ thống Chính - Tà và Chỉ số Tông Môn mở rộng
ALTER TABLE players ADD COLUMN IF NOT EXISTS alignment INT DEFAULT 0; -- 0: Neutral, 1: Righteous, 2: Evil
ALTER TABLE players ADD COLUMN IF NOT EXISTS karma INT DEFAULT 0;      -- Điểm thiện/ác
ALTER TABLE players ADD COLUMN IF NOT EXISTS alliance_id BIGINT;

-- 1. Bảng Liên Minh Tông Môn (Alliance)
CREATE TABLE IF NOT EXISTS alliances (
    id BIGSERIAL PRIMARY KEY,
    name VARCHAR(100) UNIQUE NOT NULL,
    leader_id BIGINT NOT NULL REFERENCES players(id),
    level INT DEFAULT 1,
    alignment INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 2. Bảng Trang bị và Vật phẩm (Đã có player_inventory, giờ thêm bảng instance cho trang bị)
CREATE TABLE IF NOT EXISTS item_instances (
    id BIGSERIAL PRIMARY KEY,
    player_id BIGINT NOT NULL REFERENCES players(id) ON DELETE CASCADE,
    item_code VARCHAR(50) NOT NULL,
    stats JSONB, -- Lưu chỉ số ngẫu nhiên khi rớt đồ/chế đồ
    is_equipped BOOLEAN DEFAULT FALSE,
    equipped_disciple_id BIGINT, -- ID đệ tử đang mặc
    slot VARCHAR(20) -- weapon, armor, etc.
);

-- 3. Bảng Công thức Chế tạo (Recipe)
CREATE TABLE IF NOT EXISTS crafting_recipes (
    id SERIAL PRIMARY KEY,
    recipe_code VARCHAR(50) UNIQUE NOT NULL,
    result_item_code VARCHAR(50) NOT NULL,
    materials JSONB NOT NULL, -- {"iron": 5, "wood": 2}
    success_rate INT DEFAULT 100,
    min_alignment INT DEFAULT 0 -- Một số đồ chỉ Chính hoặc Tà mới chế được
);

-- Dữ liệu mẫu Chính - Tà
INSERT INTO items (item_code, name, item_type, rarity) VALUES 
('righteous_sword', 'Chánh Khí Kiếm', 'equipment', 'epic'),
('evil_blade', 'Huyết Ma Đao', 'equipment', 'epic')
ON CONFLICT (item_code) DO NOTHING;
