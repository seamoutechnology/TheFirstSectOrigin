-- Migration 003: Thêm các cột phục vụ giám sát và autoscaling máy chủ
ALTER TABLE zones ADD COLUMN IF NOT EXISTS cpu_usage NUMERIC(5,2) DEFAULT 0.0;
ALTER TABLE zones ADD COLUMN IF NOT EXISTS ram_usage NUMERIC(5,2) DEFAULT 0.0;
ALTER TABLE zones ADD COLUMN IF NOT EXISTS player_count INT DEFAULT 0;
ALTER TABLE zones ADD COLUMN IF NOT EXISTS max_players INT DEFAULT 1000;

-- Cập nhật tên của máy chủ đầu tiên theo cụm Thanh Long 1
UPDATE zones SET name = 'Thanh Long 1' WHERE id = 1;
