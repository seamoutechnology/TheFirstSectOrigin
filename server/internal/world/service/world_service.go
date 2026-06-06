package service

import (
	"context"
	"errors"
	"fmt"
	"math/rand"
	"time"

	"server/internal/world/repository"
)

const (
	ErrCodeSuccess      = int32(0)
	ErrCodeNotFound     = int32(1001)
	ErrCodeAlreadyExist = int32(1002)
	ErrCodeNotEnough    = int32(1003)
	ErrCodeBusy         = int32(1004) // Đang nâng cấp
	ErrCodeInternal     = int32(5000)

	GoldPerLevelPerHour = 10
	MaxAFKHours         = 12

	UpgradeCostBase = 500

	UpgradeTimeBase = 60
)

type WorldService struct {
	repo     repository.IPlayerRepository
	serverID string
}

func New(repo repository.IPlayerRepository, serverID string) *WorldService {
	return &WorldService{repo: repo, serverID: serverID}
}


func (s *WorldService) CreatePlayer(ctx context.Context, userID int64, nickname string) (*repository.Player, int32, string) {
	if _, err := s.repo.FindByUserID(ctx, userID, s.serverID); !errors.Is(err, repository.ErrNotFound) {
		return nil, ErrCodeAlreadyExist, "nhân vật đã tồn tại trong server này"
	}
	if _, err := s.repo.FindByNickname(ctx, nickname, s.serverID); !errors.Is(err, repository.ErrNotFound) {
		return nil, ErrCodeAlreadyExist, "tên nhân vật đã được sử dụng"
	}
	player, err := s.repo.Create(ctx, userID, s.serverID, nickname)
	if err != nil {
		return nil, ErrCodeInternal, fmt.Sprintf("internal: %v", err)
	}
	if err = s.repo.InitPlayerBuildings(ctx, player.ID); err != nil {
		return nil, ErrCodeInternal, fmt.Sprintf("init buildings: %v", err)
	}
	emptySlots := make(map[int32]int64)
	_ = s.repo.SetFormation(ctx, player.ID, emptySlots)
	return player, ErrCodeSuccess, "Tạo nhân vật thành công"
}

func (s *WorldService) GetPlayerProfile(ctx context.Context, userID int64) (*repository.Player, int32, string) {
	player, err := s.repo.FindByUserID(ctx, userID, s.serverID)
	if err != nil {
		if errors.Is(err, repository.ErrNotFound) {
			return nil, ErrCodeNotFound, "nhân vật chưa tồn tại, hãy tạo mới"
		}
		return nil, ErrCodeInternal, fmt.Sprintf("internal: %v", err)
	}
	return player, ErrCodeSuccess, ""
}


func (s *WorldService) GetBase(ctx context.Context, userID int64) ([]*repository.PlayerBuilding, int32, string) {
	player, code, msg := s.GetPlayerProfile(ctx, userID)
	if code != ErrCodeSuccess {
		return nil, code, msg
	}
	buildings, err := s.repo.GetPlayerBuildings(ctx, player.ID)
	if err != nil {
		return nil, ErrCodeInternal, err.Error()
	}
	return buildings, ErrCodeSuccess, ""
}

func (s *WorldService) UpgradeBuilding(ctx context.Context, userID int64, buildingCode string) (*repository.PlayerBuilding, *repository.Player, int32, string) {
	player, code, msg := s.GetPlayerProfile(ctx, userID)
	if code != ErrCodeSuccess {
		return nil, nil, code, msg
	}

	buildings, _ := s.repo.GetPlayerBuildings(ctx, player.ID)
	var current *repository.PlayerBuilding
	for _, b := range buildings {
		if b.BuildingCode == buildingCode {
			current = b
			break
		}
	}
	if current == nil {
		return nil, nil, ErrCodeNotFound, "không tìm thấy công trình"
	}
	if current.UpgradeEndAt != nil && time.Now().Before(*current.UpgradeEndAt) {
		return nil, nil, ErrCodeBusy, "công trình đang được nâng cấp"
	}
	if current.Level >= int32(current.MaxLevel) {
		return nil, nil, ErrCodeBusy, "công trình đã đạt cấp tối đa"
	}

	cost := int64(current.Level) * UpgradeCostBase
	if player.Gold < cost {
		return nil, nil, ErrCodeNotEnough, fmt.Sprintf("không đủ vàng (cần %d)", cost)
	}

	updatedPlayer, err := s.repo.UpdateResources(ctx, player.ID, -cost, 0)
	if err != nil {
		return nil, nil, ErrCodeInternal, err.Error()
	}

	upgradeTime := time.Duration(int(current.Level)*UpgradeTimeBase) * time.Second
	endAt := time.Now().Add(upgradeTime)
	building, err := s.repo.UpgradeBuilding(ctx, player.ID, buildingCode, endAt)
	if err != nil {
		return nil, nil, ErrCodeInternal, err.Error()
	}

	return building, updatedPlayer, ErrCodeSuccess, ""
}

func (s *WorldService) CollectResources(ctx context.Context, userID int64, buildingCode string) (int64, *repository.Player, int32, string) {
	player, code, msg := s.GetPlayerProfile(ctx, userID)
	if code != ErrCodeSuccess {
		return 0, nil, code, msg
	}

	goldGained, err := s.repo.CollectGold(ctx, player.ID, buildingCode)
	if err != nil {
		return 0, nil, ErrCodeInternal, err.Error()
	}
	if goldGained <= 0 {
		return 0, player, ErrCodeSuccess, "chưa có vàng để thu thập"
	}

	updatedPlayer, err := s.repo.UpdateResources(ctx, player.ID, goldGained, 0)
	if err != nil {
		return 0, nil, ErrCodeInternal, err.Error()
	}
	return goldGained, updatedPlayer, ErrCodeSuccess, ""
}


func (s *WorldService) GetHeroes(ctx context.Context, userID int64) ([]*repository.PlayerHero, int32, string) {
	player, code, msg := s.GetPlayerProfile(ctx, userID)
	if code != ErrCodeSuccess {
		return nil, code, msg
	}
	heroes, err := s.repo.GetPlayerHeroes(ctx, player.ID)
	if err != nil {
		return nil, ErrCodeInternal, err.Error()
	}
	return heroes, ErrCodeSuccess, ""
}

func (s *WorldService) SetFormation(ctx context.Context, userID int64, slots map[int32]int64) (int32, string) {
	player, code, msg := s.GetPlayerProfile(ctx, userID)
	if code != ErrCodeSuccess {
		return code, msg
	}
	if err := s.repo.SetFormation(ctx, player.ID, slots); err != nil {
		return ErrCodeInternal, err.Error()
	}
	return ErrCodeSuccess, ""
}


func (s *WorldService) GetGachaBanners(ctx context.Context) ([]*repository.GachaBanner, int32, string) {
	banners, err := s.repo.GetActiveBanners(ctx)
	if err != nil {
		return nil, ErrCodeInternal, err.Error()
	}
	return banners, ErrCodeSuccess, ""
}

func (s *WorldService) DoGacha(ctx context.Context, userID int64, bannerID int32, count int32) ([]*repository.PlayerHero, *repository.Player, int32, string) {
	if count != 1 && count != 10 {
		return nil, nil, ErrCodeNotFound, "count phải là 1 hoặc 10"
	}

	player, code, msg := s.GetPlayerProfile(ctx, userID)
	if code != ErrCodeSuccess {
		return nil, nil, code, msg
	}

	banners, err := s.repo.GetActiveBanners(ctx)
	if err != nil {
		return nil, nil, ErrCodeInternal, err.Error()
	}
	var banner *repository.GachaBanner
	for _, b := range banners {
		if b.ID == bannerID {
			banner = b
			break
		}
	}
	if banner == nil {
		return nil, nil, ErrCodeNotFound, "banner không tồn tại hoặc đã kết thúc"
	}

	totalCost := int64(banner.CostDiamond) * int64(count)
	if player.Diamond < totalCost {
		return nil, nil, ErrCodeNotEnough, fmt.Sprintf("không đủ kim cương (cần %d)", totalCost)
	}

	pool, err := s.repo.GetGachaHeroPool(ctx)
	if err != nil {
		return nil, nil, ErrCodeInternal, err.Error()
	}

	pityCount, err := s.repo.GetOrCreatePity(ctx, player.ID, bannerID)
	if err != nil {
		return nil, nil, ErrCodeInternal, err.Error()
	}

	var results []*repository.PlayerHero
	rng := rand.New(rand.NewSource(time.Now().UnixNano()))
	for i := int32(0); i < count; i++ {
		pityCount++
		heroCode := rollHero(pool, pityCount, banner.PityCount, rng)
		hero, err := s.repo.AddHero(ctx, player.ID, heroCode)
		if err != nil {
			return nil, nil, ErrCodeInternal, err.Error()
		}
		for _, t := range pool {
			if t.Code == heroCode {
				hero.Name = t.Name
				hero.Rarity = t.Rarity
				hero.Element = t.Element
				hero.Role = t.Role
				hero.BaseHP = t.BaseHP
				hero.BaseATK = t.BaseATK
				hero.BaseDEF = t.BaseDEF
				hero.BaseSpeed = t.BaseSpeed
				break
			}
		}

		if hero.Rarity == "SSR" || hero.Rarity == "UR" {
			pityCount = 0
		}
		results = append(results, hero)
	}

	_ = s.repo.UpdatePity(ctx, player.ID, bannerID, pityCount)
	updatedPlayer, err := s.repo.UpdateResources(ctx, player.ID, 0, -totalCost)
	if err != nil {
		return nil, nil, ErrCodeInternal, err.Error()
	}

	return results, updatedPlayer, ErrCodeSuccess, ""
}

func rollHero(pool []*repository.HeroTemplate, pityCount, pityLimit int32, rng *rand.Rand) string {
	if pityCount >= pityLimit {
		var ssrPool []*repository.HeroTemplate
		for _, h := range pool {
			if h.Rarity == "SSR" || h.Rarity == "UR" {
				ssrPool = append(ssrPool, h)
			}
		}
		if len(ssrPool) > 0 {
			return ssrPool[rng.Intn(len(ssrPool))].Code
		}
	}

	totalWeight := int32(0)
	for _, h := range pool {
		totalWeight += h.GachaWeight
	}
	r := rng.Int31n(totalWeight)
	cumulative := int32(0)
	for _, h := range pool {
		cumulative += h.GachaWeight
		if r < cumulative {
			return h.Code
		}
	}
	return pool[0].Code
}

func (s *WorldService) GetVersionConfig(ctx context.Context, platform string) (*repository.VersionConfig, error) {
	return s.repo.GetVersionConfig(ctx, platform)
}


func (s *WorldService) SaveCutscene(ctx context.Context, id string, jsonData string) error {
	return s.repo.SaveCutscene(ctx, id, jsonData)
}

func (s *WorldService) GetCutscene(ctx context.Context, id string) (string, error) {
	return s.repo.GetCutscene(ctx, id)
}

func (s *WorldService) ListCutscenes(ctx context.Context) ([]string, error) {
	return s.repo.ListCutscenes(ctx)
}


func (s *WorldService) SaveAdminMap(ctx context.Context, id string, jsonData string) error {
	return s.repo.SaveAdminMap(ctx, id, jsonData)
}

func (s *WorldService) GetPlayerMapWithFallback(ctx context.Context, userID int64) (string, error) {
	player, err := s.repo.FindByUserID(ctx, userID, s.serverID)
	if err != nil {
		adminData, errAdmin := s.repo.GetAdminMap(ctx, "default_base")
		if errAdmin != nil {
			return "", errAdmin
		}
		return adminData, nil
	}

	playerID := player.ID
	jsonData, err := s.repo.GetPlayerMap(ctx, playerID)
	if err != nil && (err.Error() == "sql: no rows in result set" || err.Error() == "not found") {
		adminData, errAdmin := s.repo.GetAdminMap(ctx, "default_base")
		if errAdmin != nil {
			return "", errAdmin
		}
		s.repo.SavePlayerMap(ctx, playerID, adminData)
		return adminData, nil
	}
	return jsonData, err
}
