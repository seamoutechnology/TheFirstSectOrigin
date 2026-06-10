package repository

import "time"

type DBPlayerMission struct {
	PlayerID        int64     `db:"player_id"`
	MissionID       int32     `db:"mission_id"`
	Status          int32     `db:"status"`
	CurrentProgress int32     `db:"current_progress"`
	UpdatedAt       time.Time `db:"updated_at"`
}

type DBMissionTemplate struct {
	MissionID      int32     `db:"mission_id"`
	Title          string    `db:"title"`
	Description    string    `db:"description"`
	Type           int32     `db:"type"`
	TargetType     string    `db:"target_type"`
	TargetParam    string    `db:"target_param"`
	TargetProgress int32     `db:"target_progress"`
	Rewards        string    `db:"rewards"` // JSONB string
	CreatedAt      time.Time `db:"created_at"`
	UpdatedAt      time.Time `db:"updated_at"`
}
