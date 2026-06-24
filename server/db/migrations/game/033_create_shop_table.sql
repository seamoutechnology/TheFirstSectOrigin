-- +goose Up
-- SQL in this section is executed when the migration is applied.

CREATE TABLE IF NOT EXISTS shop_items (
    id            SERIAL       PRIMARY KEY,
    shop_item_id  VARCHAR(50)  NOT NULL UNIQUE,
    item_code     VARCHAR(50)  NOT NULL,
    amount        INT          NOT NULL DEFAULT 1,
    costs         JSONB        NOT NULL DEFAULT '[]' -- Array of objects: [{"item_code": "diamond", "amount": 100}]
);

INSERT INTO shop_items (shop_item_id, item_code, amount, costs) VALUES
('recruit_ticket', 'recruit_ticket', 1, '[{"item_code": "diamond", "amount": 100}]'),
('stamina_potion', 'stamina_potion', 1, '[{"item_code": "diamond", "amount": 50}]'),
('speed_hourglass', 'speed_hourglass', 1, '[{"item_code": "gold", "amount": 30}]'),
('EXP_pill_pack', 'EXP_pill', 5, '[{"item_code": "iron_ore", "amount": 10}, {"item_code": "herb_spirit", "amount": 10}]')
ON CONFLICT (shop_item_id) DO NOTHING;

-- +goose Down
-- SQL in this section is executed when the migration is rolled back.

DROP TABLE IF EXISTS shop_items;
