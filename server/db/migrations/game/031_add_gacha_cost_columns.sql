-- +goose Up
-- SQL in this section is executed when the migration is applied.

ALTER TABLE gacha_banners ADD COLUMN IF NOT EXISTS cost_item VARCHAR(50) DEFAULT '';
ALTER TABLE gacha_banners ADD COLUMN IF NOT EXISTS cost_gold INT DEFAULT 0;

-- Thêm 2 banner mẫu: Vòng quay linh thảo (yêu cầu item '00003' hoặc dùng Vàng 'cost_gold')
INSERT INTO gacha_banners (name, description, cost_diamond, pity_count, is_active, cost_item, cost_gold)
VALUES 
('Vòng Quay Linh Thảo', 'Sử dụng Linh Thảo để chiêu mộ đệ tử xuất chúng!', 0, 80, true, '00003', 0),
('Chiêu Mộ Bằng Vàng', 'Sử dụng Vàng tích luỹ để tìm kiếm đồng hành mới!', 0, 80, true, '', 5000)
ON CONFLICT DO NOTHING;

-- +goose Down
-- SQL in this section is executed when the migration is rolled back.

DELETE FROM gacha_banners WHERE cost_item = '00003' OR cost_gold = 5000;
ALTER TABLE gacha_banners DROP COLUMN IF EXISTS cost_item;
ALTER TABLE gacha_banners DROP COLUMN IF EXISTS cost_gold;
