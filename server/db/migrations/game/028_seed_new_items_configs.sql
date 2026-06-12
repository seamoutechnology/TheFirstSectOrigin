-- +goose Up
-- SQL in this section is executed when the migration is applied.

INSERT INTO item_configs (item_code, name_key, type, rarity, icon, desc_key, max_stack, required_level) VALUES
('recruit_ticket', 'recruit_ticket', 'CONSUMABLE', 'RARE', 'recruit_ticket_icon', 'recruit_ticket_des', 99, 1),
('shop_reset_ticket', 'shop_reset_ticket', 'CONSUMABLE', 'RARE', 'shop_reset_ticket_icon', 'shop_reset_ticket_des', 99, 1),
('iron_ore', 'iron_ore', 'CONSUMABLE', 'UNCOMMON', 'iron_ore_icon', 'iron_ore_des', 999, 1),
('herb_spirit', 'herb_spirit', 'CONSUMABLE', 'UNCOMMON', 'herb_spirit_icon', 'herb_spirit_des', 999, 1),
('break_pill', 'break_pill', 'CONSUMABLE', 'EPIC', 'break_pill_icon', 'break_pill_des', 99, 1),
('stamina_potion', 'stamina_potion', 'CONSUMABLE', 'RARE', 'stamina_potion_icon', 'stamina_potion_des', 99, 1),
('speed_hourglass', 'speed_hourglass', 'CONSUMABLE', 'RARE', 'speed_hourglass_icon', 'speed_hourglass_des', 99, 1),
('guild_coin', 'guild_coin', 'CURRENCY', 'RARE', 'guild_coin_icon', 'guild_coin_des', 9999, 1),
('artifact_shard', 'artifact_shard', 'CONSUMABLE', 'LEGENDARY', 'artifact_shard_icon', 'artifact_shard_des', 99, 1),
('EXP_pill', 'EXP_pill', 'CONSUMABLE', 'RARE', 'EXP_pill_icon', 'EXP_pill_des', 99, 1),
('00001', 'gold', 'CURRENCY', 'COMMON', 'gold_icon', 'gold_des', 999999, 1),
('00000', 'coin', 'CURRENCY', 'COMMON', 'coin_icon', 'coin_des', 999999, 1),
('stamina', 'stamina', 'CURRENCY', 'COMMON', 'stamina_icon', 'stamina_des', 999999, 1)
ON CONFLICT (item_code) DO UPDATE 
SET name_key = EXCLUDED.name_key,
    type = EXCLUDED.type,
    rarity = EXCLUDED.rarity,
    icon = EXCLUDED.icon,
    desc_key = EXCLUDED.desc_key,
    required_level = EXCLUDED.required_level;

-- +goose Down
-- SQL in this section is executed when the migration is rolled back.
DELETE FROM item_configs WHERE item_code IN (
  'recruit_ticket', 'shop_reset_ticket', 'iron_ore', 'herb_spirit', 'break_pill',
  'stamina_potion', 'speed_hourglass', 'guild_coin', 'artifact_shard', 'EXP_pill'
);
