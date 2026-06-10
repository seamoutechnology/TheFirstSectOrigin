package admin

import (
	"time"
)

type AnnouncementType string

const (
	TypeMaintenance AnnouncementType = "MAINTENANCE"
	TypeRules       AnnouncementType = "RULES"
	TypeActivity    AnnouncementType = "ACTIVITY"
	TypeNews        AnnouncementType = "NEWS"
)

type Announcement struct {
	ID        int              `json:"id" db:"id"`
	Type      AnnouncementType `json:"type" db:"type"`
	Title     string           `json:"title" db:"title"`
	Content   string           `json:"content" db:"content"`
	StartAt   time.Time        `json:"start_at" db:"start_at"`
	EndAt     time.Time        `json:"end_at" db:"end_at"`
	IsActive  bool             `json:"is_active" db:"is_active"`
	CreatedAt time.Time        `json:"created_at" db:"created_at"`
}

type AnnouncementRequest struct {
	ID       int              `json:"id"`
	Type     AnnouncementType `json:"type"`
	Title    string           `json:"title"`
	Content  string           `json:"content"`
	StartAt  string           `json:"start_at"` // ISO8601
	EndAt    string           `json:"end_at"`   // ISO8601
	IsActive bool             `json:"is_active"`
}

type ZoneReq struct {
	Type  string `json:"type"`   // "meta", "data"
	TabID string `json:"tab_id"` // "recent", "my_chars", "1", "2"...
}

type TabInfo struct {
	ID   string `json:"id"`
	Name string `json:"name"`
}

type MetaResponse struct {
	Tabs []TabInfo `json:"tabs"`
}

type ZoneResponse struct {
	Name            string `json:"name"`
	Host            string `json:"host"`
	Port            int    `json:"port"`
	IsOnline        bool   `json:"is_online"`
	HasCharacter    bool   `json:"has_character"`
	CharacterName   string `json:"character_name"`
	CharacterLevel  int    `json:"character_level"`
	CharacterAvatar string `json:"character_avatar"`
}

type DataResponse struct {
	Zones []ZoneResponse `json:"zones"`
}


type UserListItem struct {
	UserID   int64  `json:"user_id" db:"id"` // user_id is named id in global DB
	Email    string `json:"email" db:"email"`
	Nickname string `json:"nickname" db:"nickname"`
	Level    int    `json:"level" db:"level"`
}

type UserListResponse struct {
	Total int            `json:"total"`
	Page  int            `json:"page"`
	Data  []UserListItem `json:"data"`
}

type UserInfo struct {
	UserID    int64  `json:"user_id"`
	Email     string `json:"email"`
	SectName  string `json:"sect_name"`
	Level     int    `json:"level"`
	Money     int    `json:"money"`
}

type UserItem struct {
	ID       int64  `json:"id" db:"id"`
	PlayerID int64  `json:"player_id" db:"player_id"`
	ItemCode string `json:"item_code" db:"item_code"`
	Quantity int    `json:"quantity" db:"quantity"`
}

type AddItemRequest struct {
	ItemCode string `json:"item_code"`
	Quantity int    `json:"quantity"`
}

type ItemConfigData struct {
	ItemCode    string `json:"item_code" db:"item_code"`
	NameKey     string `json:"name_key" db:"name_key"`
	Type        string `json:"type" db:"type"`
	Rarity      string `json:"rarity" db:"rarity"`
	Icon        string `json:"icon" db:"icon"`
	DescKey     string `json:"desc_key" db:"desc_key"`
	MaxStack    int    `json:"max_stack" db:"max_stack"`
	Sources     string `json:"sources" db:"sources"` // JSONB array of ItemSource
	Effects     string `json:"effects" db:"effects"` // JSONB array of ItemEffect
}

type EffectConfigData struct {
	EffectCode string  `json:"effect_code" db:"effect_code"`
	NameKey    string  `json:"name_key" db:"name_key"`
	DescKey    string  `json:"desc_key" db:"desc_key"`
	EffectType string  `json:"effect_type" db:"effect_type"`
	ValueType  string  `json:"value_type" db:"value_type"` // 'flat' or 'percent'
	MinValue   float64 `json:"min_value" db:"min_value"`
	MaxValue   float64 `json:"max_value" db:"max_value"`
	SourceStat string  `json:"source_stat" db:"source_stat"`
}

type FeatureConfigData struct {
	FeatureCode          string `json:"feature_code" db:"feature_code"`
	NameKey              string `json:"name_key" db:"name_key"`
	Icon                 string `json:"icon" db:"icon"`
	RequiredPlayerLevel  int32  `json:"required_player_level" db:"required_player_level"`
	RequiredMissionCode  string `json:"required_mission_code" db:"required_mission_code"`
	IsActive             bool   `json:"is_active" db:"is_active"`
}

type MissionTemplateData struct {
	MissionID      int32  `json:"mission_id" db:"mission_id"`
	Title          string `json:"title" db:"title"`
	Description    string `json:"description" db:"description"`
	Type           int32  `json:"type" db:"type"`
	TargetType     string `json:"target_type" db:"target_type"`
	TargetParam    string `json:"target_param" db:"target_param"`
	TargetProgress int32  `json:"target_progress" db:"target_progress"`
	Rewards        string `json:"rewards" db:"rewards"` // JSON string
}

type StageConfigDB struct {
	StageID   string `json:"stage_id" db:"stage_id"`
	JSONData  string `json:"json_data" db:"json_data"`
	UpdatedAt string `json:"updated_at" db:"updated_at"`
}

type SyncStageReq struct {
	StageID  string `json:"stage_id"`
	JSONData string `json:"json_data"`
}

type TraitConfigDB struct {
	TraitCode string `json:"trait_code" db:"trait_code"`
	Weight    int    `json:"weight" db:"weight"`
	JSONData  string `json:"json_data" db:"json_data"`
	UpdatedAt string `json:"updated_at" db:"updated_at"`
}

type SyncTraitReq struct {
	TraitCode string `json:"trait_code"`
	Weight    int    `json:"weight"`
	JSONData  string `json:"json_data"`
}

