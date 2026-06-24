-- +goose Up
-- SQL in this section is executed when the migration is applied.

DROP TABLE IF EXISTS shop_items;

CREATE TABLE shop_items (
    id              SERIAL       PRIMARY KEY,
    shop_item_id    VARCHAR(50)  NOT NULL UNIQUE,
    shop_type       VARCHAR(50)  NOT NULL,
    item_code       VARCHAR(50)  NOT NULL,
    amount          INT          NOT NULL DEFAULT 1,
    original_price  JSONB        NOT NULL DEFAULT '[]',
    is_discountable BOOLEAN      NOT NULL DEFAULT TRUE
);

CREATE TABLE player_shops (
    id              SERIAL       PRIMARY KEY,
    player_id       INT          NOT NULL,
    shop_type       VARCHAR(50)  NOT NULL,
    shop_item_id    VARCHAR(50)  NOT NULL,
    item_code       VARCHAR(50)  NOT NULL,
    amount          INT          NOT NULL,
    final_price     JSONB        NOT NULL,
    discount_pct    INT          NOT NULL DEFAULT 0,
    is_bought       BOOLEAN      NOT NULL DEFAULT FALSE,
    updated_at      TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE TABLE player_shop_resets (
    player_id       INT          NOT NULL,
    shop_type       VARCHAR(50)  NOT NULL,
    next_refresh_at TIMESTAMPTZ  NOT NULL,
    PRIMARY KEY (player_id, shop_type)
);

INSERT INTO shop_items (shop_item_id, shop_type, item_code, amount, original_price, is_discountable) VALUES
('daily_recruit_ticket', 'daily', 'recruit_ticket', 1, '[{"item_code": "diamond", "amount": 100}]', true),
('daily_stamina_potion', 'daily', 'stamina_potion', 1, '[{"item_code": "diamond", "amount": 50}]', true),
('daily_speed_hourglass', 'daily', 'speed_hourglass', 1, '[{"item_code": "gold", "amount": 30}]', true),
('daily_EXP_pill', 'daily', 'EXP_pill', 5, '[{"item_code": "iron_ore", "amount": 10}]', true),
('guild_recruit_ticket', 'guild', 'recruit_ticket', 1, '[{"item_code": "guild_coin", "amount": 200}]', false),
('guild_artifact_shard', 'guild', 'artifact_shard', 2, '[{"item_code": "guild_coin", "amount": 500}]', false)
ON CONFLICT (shop_item_id) DO NOTHING;

-- +goose Down
-- SQL in this section is executed when the migration is rolled back.

DROP TABLE IF EXISTS player_shop_resets;
DROP TABLE IF EXISTS player_shops;
DROP TABLE IF EXISTS shop_items;
