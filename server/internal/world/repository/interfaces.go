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
	UpgradeBuilding(ctx context.Context, playerID int64, code string, endAt time.Time) (*PlayerBuilding, error)
	CollectGold(ctx context.Context, playerID int64, code string) (int64, error)
	GetPlayerHeroes(ctx context.Context, playerID int64) ([]*PlayerHero, error)
	AddHero(ctx context.Context, playerID int64, heroCode string) (*PlayerHero, error)
	GetActiveBanners(ctx context.Context) ([]*GachaBanner, error)
	GetGachaHeroPool(ctx context.Context) ([]*HeroTemplate, error)
	GetOrCreatePity(ctx context.Context, playerID int64, bannerID int32) (int32, error)
	UpdatePity(ctx context.Context, playerID int64, bannerID int32, newCount int32) error
	SetFormation(ctx context.Context, playerID int64, slots map[int32]int64) error
	GetVersionConfig(ctx context.Context, platform string) (*VersionConfig, error)
	
	SaveCutscene(ctx context.Context, id string, jsonData string) error
	GetCutscene(ctx context.Context, id string) (string, error)
	ListCutscenes(ctx context.Context) ([]string, error)
	
	SaveAdminMap(ctx context.Context, id string, jsonData string) error
	GetAdminMap(ctx context.Context, id string) (string, error)
	SavePlayerMap(ctx context.Context, playerID int64, jsonData string) error
	GetPlayerMap(ctx context.Context, playerID int64) (string, error)
}
