-- Bổ sung thuộc tính cho Chưởng Môn (Nhân vật chính)
ALTER TABLE players 
ADD COLUMN IF NOT EXISTS avatar VARCHAR(255) DEFAULT 'default_avatar',
ADD COLUMN IF NOT EXISTS skin_id VARCHAR(100) DEFAULT 'default_skin',
ADD COLUMN IF NOT EXISTS title VARCHAR(100) DEFAULT 'Tân Thủ Chưởng Môn',
ADD COLUMN IF NOT EXISTS power BIGINT DEFAULT 0,
ADD COLUMN IF NOT EXISTS karma INTEGER DEFAULT 0; -- Karma quyết định alignment
