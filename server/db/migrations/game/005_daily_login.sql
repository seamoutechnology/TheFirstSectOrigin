-- Quản lý điểm danh hằng ngày
CREATE TABLE IF NOT EXISTS player_daily_rewards (
    player_id BIGINT REFERENCES players(id),
    reward_day INT NOT NULL, -- 1 đến 7
    claimed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (player_id, reward_day)
);
