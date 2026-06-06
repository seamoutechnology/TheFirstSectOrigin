package combat

import (
	"time"
)

type BattleType string

const (
	TypePvE      BattleType = "PVE"
	TypePvP      BattleType = "PVP"
	TypeBoss     BattleType = "BOSS"
	TypeWorldBoss BattleType = "WORLD_BOSS"
)

type BattleConfig struct {
	Type          BattleType `json:"type"`
	MaxTurns      int        `json:"max_turns"`
	WinCondition  string     `json:"win_condition"` // "KILL_ALL", "SURVIVE", "DEAL_DAMAGE"
	TimeLimit     int        `json:"time_limit"`     // giây
}

type Position struct {
	X int `json:"x"`
	Y int `json:"y"`
}

type Unit struct {
	ID        string    `json:"id"`
	BaseID    string    `json:"base_id"`
	Name      string    `json:"name"`
	Star      int       `json:"star"`
	HP        int       `json:"hp"`
	MaxHP     int       `json:"max_hp"`
	Atk       int       `json:"atk"`
	Def       int       `json:"def"`
	Pos       Position  `json:"pos"`
	IsEnemy   bool      `json:"is_enemy"`
	SkillID   string    `json:"skill_id"`
	Buffs     []*Buff   `json:"buffs"`
	IsStunned bool      `json:"is_stunned"` // Bị đóng băng/choáng
	TargetingRules []TargetingRule `json:"targeting_rules"` // Danh sách ưu tiên
}

type TargetingRule struct {
	Type   string  `json:"type"`   // "FROZEN", "BACKLINE", "LOWEST_HP", "DISTANCE"
	Weight float64 `json:"weight"` // Trọng số (Điểm cộng/trừ)
}

type Buff struct {
	Type     string `json:"type"` // "POISON", "FREEZE", "SLOW", "SHIELD"
	Value    int    `json:"value"`
	Duration int    `json:"duration"` // Số lượt còn lại
}

type BattleState struct {
	BattleID  string        `json:"battle_id"`
	Config    BattleConfig  `json:"config"`
	Units     []*Unit       `json:"units"`
	Turn      int           `json:"turn"`
	History   []Event       `json:"history"`
	StartTime time.Time     `json:"start_time"`
	TotalDamage int64       `json:"total_damage"` // Dùng cho World Boss
}

type Event struct {
	Type     string      `json:"type"` // "ATTACK", "SKILL", "DIE", "MOVE"
	SourceID string      `json:"source_id"`
	TargetID string      `json:"target_id"`
	Value    int         `json:"value"` // Sát thương hoặc hồi máu
	Effect   string      `json:"effect"`
}
