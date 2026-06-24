-- +goose Up
-- SQL in this section is executed when the migration is applied.

UPDATE item_configs
SET effects = '[{"effect_code": "EFF_ADD_STAMINA", "value": 50}]'::jsonb
WHERE item_code = 'stamina_potion';

UPDATE item_configs
SET effects = '[{"effect_code": "EFF_ADD_EXP", "value": 100}]'::jsonb
WHERE item_code = 'EXP_pill';

-- +goose Down
-- SQL in this section is executed when the migration is rolled back.

UPDATE item_configs
SET effects = '[]'::jsonb
WHERE item_code IN ('stamina_potion', 'EXP_pill');
