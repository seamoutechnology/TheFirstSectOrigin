-- +goose Up
-- SQL in this section is executed when the migration is applied.

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

-- +goose Down
-- SQL in this section is executed when the migration is rolled back.

DELETE FROM skills WHERE skill_code IN (
    'skill_shadow_strike', 'skill_slash', 'skill_blood_claw',
    'skill_fireball', 'skill_shield', 'skill_meteor',
    'skill_blade_tempest', 'skill_heal', 'skill_divine_light',
    'skill_lightning_storm', 'skill_frozen_armor', 'skill_holy_revive'
);
