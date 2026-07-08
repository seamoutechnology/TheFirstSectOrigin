package repository

import (
	"context"
	"time"
)

type IPlayerRepository interface {
	Create(ctx context.Context, userID int64, serverID string, nickname string) (*Player, error)
	FindByUserID(ctx context.Context, userID int64, serverID string) (*Player, error)
	FindByNickname(ctx context.Context, nickname string, serverID string) (*Player, error)
	UpdateResources(ctx context.Context, playerID int64, goldDelta, diamondDelta int64) (*Player, error)
	InitPlayerBuildings(ctx context.Context, playerID int64) error
	GetPlayerBuildings(ctx context.Context, playerID int64) ([]*PlayerBuilding, error)
	UpgradeBuilding(ctx context.Context, playerID int64, instanceID int64, endAt time.Time) (*PlayerBuilding, error)
	SpeedUpBuilding(ctx context.Context, playerID int64, instanceID int64, diamondCost int64) (*PlayerBuilding, *Player, error)
	CollectGold(ctx context.Context, playerID int64, instanceID int64) (int64, error)
	AddPlayerBuilding(ctx context.Context, playerID int64, buildingCode string) (*PlayerBuilding, error)
	DeletePlayerBuilding(ctx context.Context, playerID int64, instanceID int64) error
	GetPlayerHeroes(ctx context.Context, playerID int64) ([]*PlayerHero, error)
	AddHero(ctx context.Context, playerID int64, heroCode string) (*PlayerHero, error)
	LevelUpHero(ctx context.Context, playerID int64, heroID int64) (*PlayerHero, error)
	GetActiveBanners(ctx context.Context) ([]*GachaBanner, error)
	GetGachaHeroPool(ctx context.Context) ([]*HeroTemplate, error)
	GetOrCreatePity(ctx context.Context, playerID int64, bannerID int32) (int32, error)
	UpdatePity(ctx context.Context, playerID int64, bannerID int32, newCount int32) error
	SetFormation(ctx context.Context, playerID int64, slots map[int32]int64) error
	GetFormation(ctx context.Context, playerID int64) (map[int32]int64, error)
	GetVersionConfig(ctx context.Context, platform string) (*VersionConfig, error)
	
	SaveCutscene(ctx context.Context, id string, jsonData string) error
	GetCutscene(ctx context.Context, id string) (string, error)
	ListCutscenes(ctx context.Context) ([]string, error)
	
	SaveAdminMap(ctx context.Context, id string, jsonData string) error
	GetAdminMap(ctx context.Context, id string) (string, error)
	SavePlayerMap(ctx context.Context, playerID int64, jsonData string) error
	GetPlayerMap(ctx context.Context, playerID int64) (string, error)

	GetPlayerInventory(ctx context.Context, playerID int64) ([]*UserItem, error)
	DeductUserItems(ctx context.Context, playerID int64, items map[string]int32) error
	
	GetPlayerItemInstance(ctx context.Context, playerID int64, itemID int64) (*UserItem, error)
	GetItemConfig(ctx context.Context, itemCode string) (*RepoItemConfig, error)
	GetAllItemConfigs(ctx context.Context) ([]*RepoItemConfig, error)
	UseItemTransaction(ctx context.Context, playerID int64, itemID int64, quantity int32, useType string, codeParam string, valueParam int32) error

	GetMissionTemplates(ctx context.Context) ([]*DBMissionTemplate, error)
	GetPlayerMissions(ctx context.Context, playerID int64) ([]*DBPlayerMission, error)
	UpdatePlayerMissionProgress(ctx context.Context, playerID int64, missionID int32, progress int32, status int32) error
	CreatePlayerMission(ctx context.Context, playerID int64, missionID int32, status int32) error
	ClaimMissionRewardDB(ctx context.Context, playerID int64, missionID int32, rewards map[string]int32) error
	GetSkillConfigs(ctx context.Context) ([]*SkillConfig, error)
	ProcessPvECombatResult(ctx context.Context, playerID int64, stageID string, isVictory bool, rewardExp int32, rewardLinhThach int32) (*Player, error)
	GetCompletedStages(ctx context.Context, playerID int64) ([]string, error)
	GetLeaderboard(ctx context.Context, leaderboardType string) ([]*LeaderboardRecord, error)
	CollectResource(ctx context.Context, playerID int64, instanceID int64, itemCode string, amount int64) error
	UpdatePlayerPower(ctx context.Context, playerID int64) error
	GetShopItem(ctx context.Context, shopItemID string) (*ShopItem, error)
	GetShopItemsByType(ctx context.Context, shopType string) ([]*ShopItem, error)
	GetPlayerShopItems(ctx context.Context, playerID int64, shopType string) ([]*PlayerShopItemInstance, time.Time, error)
	SavePlayerShopItems(ctx context.Context, playerID int64, shopType string, items []*PlayerShopItemInstance, nextRefresh time.Time) error
	BuyPlayerShopItemTransaction(ctx context.Context, playerID int64, instanceID int64, quantity int32) ([]*UserItem, error)
	InsertShopItem(ctx context.Context, item *ShopItem) error
	DeleteShopItem(ctx context.Context, shopItemID string) error
	GetAllShopItems(ctx context.Context) ([]*ShopItem, error)
}

type LeaderboardRecord struct {
	Rank     int64
	Nickname string
	Value    int64
}

type SkillConfig struct {
	SkillCode        string  `json:"skill_code"`
	Name             string  `json:"name"`
	DamageMultiplier float64 `json:"damage_multiplier"`
	Cooldown         int32   `json:"cooldown"`
	EffectType       string  `json:"effect_type"`
}

type UserItem struct {
	ID       int64
	PlayerID int64
	ItemCode string
	Quantity int32
	Stats    string
}

type RepoItemConfig struct {
	ItemCode      string
	NameKey       string
	Type          string
	Rarity        string
	Icon          string
	DescKey       string
	MaxStack      int32
	Sources       string
	Effects       string
	RequiredLevel int32
}

type ShopItem struct {
	ID             int64
	ShopItemID     string
	ShopType       string
	ItemCode       string
	Amount         int32
	OriginalPrice  string
	IsDiscountable bool
}

type ShopCost struct {
	ItemCode string `json:"item_code"`
	Amount   int32  `json:"amount"`
}

type PlayerShopItemInstance struct {
	ID          int64
	PlayerID    int64
	ShopType    string
	ShopItemID  string
	ItemCode    string
	Amount      int32
	FinalPrice  string
	DiscountPct int32
	IsBought    bool
}



