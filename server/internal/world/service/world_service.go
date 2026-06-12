package service

import (
	"context"
	"encoding/json"
	"errors"
	"fmt"
	"math/rand"
	"os"
	"strings"
	"time"

	"server/internal/world/repository"
	pb "server/pkg/pb"
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

type BuildingRequirementJSON struct {
	ItemCode string `json:"itemCode"`
	Quantity int    `json:"quantity"`
}

type BuildingLevelJSON struct {
	Level              int                       `json:"level"`
	RequiredReputation int                       `json:"requiredReputation"`
	BuildTimeSeconds   int                       `json:"buildTimeSeconds"`
	CostItems          []BuildingRequirementJSON `json:"costItems"`
}

type BuildingDataJSON struct {
	BuildingID string              `json:"buildingID"`
	LevelStats []BuildingLevelJSON `json:"levelStats"`
}

type BuildingConfigsJSON struct {
	Buildings []BuildingDataJSON `json:"buildings"`
}

type SpeedUpConfig struct {
	FreeUnderSeconds int64 `json:"free_under_seconds"`
}

type WorldService struct {
	repo            repository.IPlayerRepository
	serverID        string
	buildingConfigs map[string]map[int]BuildingLevelJSON
	speedUpConfig   SpeedUpConfig
}

func New(repo repository.IPlayerRepository, serverID string) *WorldService {
	return &WorldService{
		repo:            repo,
		serverID:        serverID,
		buildingConfigs: make(map[string]map[int]BuildingLevelJSON),
		speedUpConfig:   SpeedUpConfig{FreeUnderSeconds: 0}, // default
	}
}

func (s *WorldService) LoadBuildingConfigs(filePath string) error {
	file, err := os.Open(filePath)
	if err != nil {
		return err
	}
	defer file.Close()

	var data BuildingConfigsJSON
	decoder := json.NewDecoder(file)
	if err := decoder.Decode(&data); err != nil {
		return err
	}

	newConfigs := make(map[string]map[int]BuildingLevelJSON)
	for _, b := range data.Buildings {
		levels := make(map[int]BuildingLevelJSON)
		for _, lvl := range b.LevelStats {
			levels[lvl.Level] = lvl
		}
		newConfigs[b.BuildingID] = levels
	}

	s.buildingConfigs = newConfigs
	return nil
}

func (s *WorldService) LoadSpeedUpConfigs(filePath string) error {
	file, err := os.Open(filePath)
	if err != nil {
		return err
	}
	defer file.Close()

	var data SpeedUpConfig
	decoder := json.NewDecoder(file)
	if err := decoder.Decode(&data); err != nil {
		return err
	}
	s.speedUpConfig = data
	return nil
}


func (s *WorldService) CreatePlayer(ctx context.Context, userID int64, nickname string) (*repository.Player, int32, string) {
	nickname = strings.TrimSpace(nickname)
	if nickname == "" {
		return nil, ErrCodeAlreadyExist, "tên nhân vật không được để trống"
	}
	if len(nickname) < 2 || len(nickname) > 20 {
		return nil, ErrCodeAlreadyExist, "tên nhân vật phải từ 2 đến 20 ký tự"
	}
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
	
	// Tự động cấp 3 tướng tân thủ mặc định cho người chơi mới
	defaultSlots := make(map[int32]int64)
	hero1, err := s.repo.AddHero(ctx, player.ID, "FIRE_WARRIOR_01")
	if err == nil && hero1 != nil {
		defaultSlots[0] = hero1.ID
	}
	hero2, err := s.repo.AddHero(ctx, player.ID, "WATER_TANK_01")
	if err == nil && hero2 != nil {
		defaultSlots[1] = hero2.ID
	}
	hero3, err := s.repo.AddHero(ctx, player.ID, "WOOD_HEALER_01")
	if err == nil && hero3 != nil {
		defaultSlots[2] = hero3.ID
	}

	_ = s.repo.SetFormation(ctx, player.ID, defaultSlots)
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

func (s *WorldService) UpgradeBuilding(ctx context.Context, userID int64, instanceID int64) (*repository.PlayerBuilding, *repository.Player, int32, string) {
	player, code, msg := s.GetPlayerProfile(ctx, userID)
	if code != ErrCodeSuccess {
		return nil, nil, code, msg
	}

	buildings, _ := s.repo.GetPlayerBuildings(ctx, player.ID)
	var current *repository.PlayerBuilding
	for _, b := range buildings {
		if b.ID == instanceID {
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

	// Xác định max level từ file json cấu hình nếu có
	maxConfigLevel := 0
	if s.buildingConfigs != nil {
		if levels, exists := s.buildingConfigs[current.BuildingCode]; exists {
			for lvl := range levels {
				if lvl > maxConfigLevel {
					maxConfigLevel = lvl
				}
			}
		}
	}

	limitLevel := int32(current.MaxLevel)
	if maxConfigLevel > 0 {
		limitLevel = int32(maxConfigLevel)
	}

	if current.Level >= limitLevel {
		return nil, nil, ErrCodeBusy, "công trình đã đạt cấp tối đa"
	}

	buildingCode := current.BuildingCode

	// 1. Kiểm tra cấu hình động
	var costItems []BuildingRequirementJSON
	var buildTimeSeconds int
	hasConfig := false

	if s.buildingConfigs != nil {
		if levels, exists := s.buildingConfigs[buildingCode]; exists {
			nextLevel := int(current.Level + 1)
			if lvlConf, lvlExists := levels[nextLevel]; lvlExists {
				costItems = lvlConf.CostItems
				buildTimeSeconds = lvlConf.BuildTimeSeconds
				hasConfig = true
			}
		}
	}

	if hasConfig {
		itemRequirements := make(map[string]int32)
		goldReq := int64(0)
		for _, req := range costItems {
			if req.ItemCode == "gold" {
				goldReq = int64(req.Quantity)
			} else {
				itemRequirements[req.ItemCode] = int32(req.Quantity)
			}
		}

		if goldReq > 0 && player.Gold < goldReq {
			return nil, nil, ErrCodeNotEnough, fmt.Sprintf("không đủ vàng (cần %d)", goldReq)
		}
		if len(itemRequirements) > 0 {
			err := s.repo.DeductUserItems(ctx, player.ID, itemRequirements)
			if err != nil {
				return nil, nil, ErrCodeNotEnough, err.Error()
			}
		}
		if goldReq > 0 {
			updatedPlayer, err := s.repo.UpdateResources(ctx, player.ID, -goldReq, 0)
			if err != nil {
				return nil, nil, ErrCodeInternal, err.Error()
			}
			player = updatedPlayer
		}

		upgradeTime := time.Duration(buildTimeSeconds) * time.Second
		endAt := time.Now().Add(upgradeTime)
		building, err := s.repo.UpgradeBuilding(ctx, player.ID, instanceID, endAt)
		if err != nil {
			return nil, nil, ErrCodeInternal, err.Error()
		}
		return building, player, ErrCodeSuccess, ""
	}

	// 2. Fallback cấu hình cũ nếu không tìm thấy file cấu hình
	goldReq := int64(current.Level * 200)
	if player.Gold < goldReq {
		return nil, nil, ErrCodeNotEnough, fmt.Sprintf("không đủ vàng (cần %d)", goldReq)
	}

	woodReq := current.Level * 100
	itemRequirements := map[string]int32{
		"wood": woodReq,
	}

	err := s.repo.DeductUserItems(ctx, player.ID, itemRequirements)
	if err != nil {
		return nil, nil, ErrCodeNotEnough, err.Error()
	}

	updatedPlayer, err := s.repo.UpdateResources(ctx, player.ID, -goldReq, 0)
	if err != nil {
		return nil, nil, ErrCodeInternal, err.Error()
	}

	upgradeTime := time.Duration(int(current.Level)*UpgradeTimeBase) * time.Second
	endAt := time.Now().Add(upgradeTime)
	building, err := s.repo.UpgradeBuilding(ctx, player.ID, instanceID, endAt)
	if err != nil {
		return nil, nil, ErrCodeInternal, err.Error()
	}

	return building, updatedPlayer, ErrCodeSuccess, ""
}

func (s *WorldService) SpeedUpBuilding(ctx context.Context, userID int64, instanceID int64) (*repository.PlayerBuilding, *repository.Player, int32, string) {
	player, code, msg := s.GetPlayerProfile(ctx, userID)
	if code != ErrCodeSuccess {
		return nil, nil, code, msg
	}

	buildings, _ := s.repo.GetPlayerBuildings(ctx, player.ID)
	var current *repository.PlayerBuilding
	for _, b := range buildings {
		if b.ID == instanceID {
			current = b
			break
		}
	}
	if current == nil {
		return nil, nil, ErrCodeNotFound, "không tìm thấy công trình"
	}
	if current.UpgradeEndAt == nil || !time.Now().Before(*current.UpgradeEndAt) {
		return nil, nil, ErrCodeBusy, "công trình không ở trong tiến trình nâng cấp"
	}

	// Calculate diamond cost: free under X seconds, otherwise subtract X and compute cost (1 diamond per 10s)
	remaining := time.Until(*current.UpgradeEndAt)
	remainingSecs := int64(remaining.Seconds())
	if remainingSecs < 0 {
		remainingSecs = 0
	}

	var diamondCost int64 = 0
	if remainingSecs > s.speedUpConfig.FreeUnderSeconds {
		y := remainingSecs - s.speedUpConfig.FreeUnderSeconds
		diamondCost = y / 10
		if y%10 != 0 || diamondCost == 0 {
			diamondCost++
		}
	}

	if player.Diamond < diamondCost {
		return nil, nil, ErrCodeNotEnough, fmt.Sprintf("không đủ kim cương (cần %d)", diamondCost)
	}

	building, updatedPlayer, err := s.repo.SpeedUpBuilding(ctx, player.ID, instanceID, diamondCost)
	if err != nil {
		return nil, nil, ErrCodeInternal, err.Error()
	}

	return building, updatedPlayer, ErrCodeSuccess, ""
}

func (s *WorldService) CollectResources(ctx context.Context, userID int64, instanceID int64) (int64, *repository.Player, int32, string) {
	player, code, msg := s.GetPlayerProfile(ctx, userID)
	if code != ErrCodeSuccess {
		return 0, nil, code, msg
	}

	goldGained, err := s.repo.CollectGold(ctx, player.ID, instanceID)
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
	
	// Auto-seeding removed to support clean custom GM add hero flow

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

type ExportedBuildingJSON struct {
	InstanceID int64  `json:"instance_id"`
	ID         string `json:"id"`
	X          int    `json:"x"`
	Y          int    `json:"y"`
	Level      int    `json:"level"`
	State      int    `json:"state"`
	FlipX      bool   `json:"flipX"`
}

type ExportedGroundLayerJSON struct {
	LayerName string   `json:"layerName"`
	Tiles     []string `json:"tiles"`
}

type MapLayoutJSON struct {
	GridWidth    int                      `json:"gridWidth"`
	GridHeight   int                      `json:"gridHeight"`
	Buildings    []ExportedBuildingJSON   `json:"buildings"`
	GroundTiles  []string                 `json:"groundTiles,omitempty"`
	GroundLayers []ExportedGroundLayerJSON `json:"groundLayers,omitempty"`
	TerrainData  []int                    `json:"terrainData"`
	FogData      []int                    `json:"fogData"`
	Items        []interface{}            `json:"items"`
}

func (s *WorldService) SavePlayerMap(ctx context.Context, userID int64, jsonData string) error {
	player, err := s.repo.FindByUserID(ctx, userID, s.serverID)
	if err != nil {
		return err
	}

	// 1. Parse JSON bản đồ gửi lên từ client
	var incoming MapLayoutJSON
	if err := json.Unmarshal([]byte(jsonData), &incoming); err != nil {
		return fmt.Errorf("cấu hình bản đồ không hợp lệ: %v", err)
	}

	// 2. Lấy danh sách các công trình thực tế mà người chơi đã sở hữu trong DB
	ownedBuildings, err := s.repo.GetPlayerBuildings(ctx, player.ID)
	if err != nil {
		return err
	}

	ownedMap := make(map[int64]*repository.PlayerBuilding)
	mainHallLevel := int32(1)
	for _, ob := range ownedBuildings {
		ownedMap[ob.ID] = ob
		if ob.BuildingCode == "main_hall" {
			mainHallLevel = ob.Level
		}
	}

	// 3. Kiểm tra tính hợp lệ của từng công trình trên bản đồ để chống Cheat
	// Đánh dấu các ID đã được map để tránh trùng lặp
	mappedIDs := make(map[int64]bool)
	for _, b := range incoming.Buildings {
		if b.InstanceID != 0 {
			mappedIDs[b.InstanceID] = true
		}
	}

	placedCount := make(map[string]int)
	for i, b := range incoming.Buildings {
		if b.InstanceID == 0 {
			// Thử tìm công trình đã sở hữu cùng loại chưa được đặt lên map
			var matched *repository.PlayerBuilding
			for _, ob := range ownedBuildings {
				if ob.BuildingCode == b.ID && !mappedIDs[ob.ID] {
					matched = ob
					break
				}
			}

			if matched != nil {
				// Khớp với công trình có sẵn trong DB
				incoming.Buildings[i].InstanceID = matched.ID
				mappedIDs[matched.ID] = true
			} else {
				// Thực sự là công trình mới mua -> Tạo dòng mới trong DB
				newB, err := s.repo.AddPlayerBuilding(ctx, player.ID, b.ID)
				if err != nil {
					return fmt.Errorf("lỗi thêm công trình mới %s: %v", b.ID, err)
				}
				incoming.Buildings[i].InstanceID = newB.ID
				ownedBuildings = append(ownedBuildings, newB)
				ownedMap[newB.ID] = newB
				mappedIDs[newB.ID] = true
			}
		}

		dbBuilding, exists := ownedMap[incoming.Buildings[i].InstanceID]
		if !exists || dbBuilding.PlayerID != player.ID {
			return fmt.Errorf("bạn chưa sở hữu công trình này: %s (ID: %d)", b.ID, b.InstanceID)
		}
		if dbBuilding.BuildingCode != b.ID {
			return fmt.Errorf("mã công trình không khớp với database: %s vs %s", b.ID, dbBuilding.BuildingCode)
		}

		placedCount[b.ID]++
		if b.ID == "main_hall" && placedCount[b.ID] > 1 {
			return fmt.Errorf("không thể đặt nhiều hơn 1 công trình thuộc loại: %s", b.ID)
		}

		targetLevel := dbBuilding.Level
		// Lazy completion check: nếu đang nâng cấp và thời gian đã hết, level thực tế của người chơi sẽ + 1
		if dbBuilding.UpgradeEndAt != nil && time.Now().After(*dbBuilding.UpgradeEndAt) {
			targetLevel++
		}

		if int32(b.Level) > targetLevel {
			return fmt.Errorf("cấp độ công trình %s không hợp lệ (client: %d, server: %d)", b.ID, b.Level, targetLevel)
		}
	}

	if mainHallLevel < 2 {
		for _, ob := range ownedBuildings {
			if placedCount[ob.BuildingCode] <= 0 {
				return fmt.Errorf("đại điện phải đạt cấp 2 mới được phép cất công trình %s vào kho", ob.BuildingCode)
			}
		}
	}

	// =========================================================
	// FIX: Giữ nguyên groundLayers và groundTiles từ map hiện tại của player.
	// Client chỉ gửi lên buildings[], không gửi groundLayers / groundTiles.
	// Nếu ta lưu thẳng JSON của client → groundLayers/groundTiles bị mất
	// → lần sau load lại map bị trắng (không có tiles nền).
	//
	// Giải pháp: lấy ground data từ player map hiện tại trong DB.
	// Nếu chưa có → lấy từ admin map mặc định.
	// Sau đó merge vào JSON trước khi lưu.
	// =========================================================
	if len(incoming.GroundLayers) == 0 && len(incoming.GroundTiles) == 0 {
		// Lấy map hiện tại của player để giữ lại ground data
		existingJSON, errExisting := s.repo.GetPlayerMap(ctx, player.ID)
		if errExisting != nil || existingJSON == "" {
			// Chưa có map riêng → lấy từ admin default
			existingJSON, _ = s.repo.GetAdminMap(ctx, "default_base")
		}

		if existingJSON != "" {
			var existingMap MapLayoutJSON
			if jsonErr := json.Unmarshal([]byte(existingJSON), &existingMap); jsonErr == nil {
				// Merge: giữ groundLayers + groundTiles + terrainData + fogData từ map cũ,
				// chỉ cập nhật buildings từ client gửi lên
				incoming.GroundLayers = existingMap.GroundLayers
				incoming.GroundTiles = existingMap.GroundTiles
				if len(incoming.TerrainData) == 0 {
					incoming.TerrainData = existingMap.TerrainData
				}
				if len(incoming.FogData) == 0 {
					incoming.FogData = existingMap.FogData
				}
			}
		}
	}

	// Serialize JSON đã merge để lưu vào DB
	mergedBytes, err := json.Marshal(incoming)
	if err != nil {
		return fmt.Errorf("lỗi serialize map: %v", err)
	}

	return s.repo.SavePlayerMap(ctx, player.ID, string(mergedBytes))
}

func (s *WorldService) GetInventory(ctx context.Context, userID int64) ([]*repository.UserItem, int32, string) {
	player, code, msg := s.GetPlayerProfile(ctx, userID)
	if code != ErrCodeSuccess {
		return nil, code, msg
	}
	items, err := s.repo.GetPlayerInventory(ctx, player.ID)
	if err != nil {
		return nil, ErrCodeInternal, err.Error()
	}
	return items, ErrCodeSuccess, ""
}

func (s *WorldService) GetItemConfig(ctx context.Context, itemCode string) (*repository.RepoItemConfig, error) {
	return s.repo.GetItemConfig(ctx, itemCode)
}

func (s *WorldService) GetAllItemConfigs(ctx context.Context) ([]*repository.RepoItemConfig, error) {
	return s.repo.GetAllItemConfigs(ctx)
}

type ServiceItemEffect struct {
	EffectCode string  `json:"effect_code"`
	Value      float64 `json:"value"`
	MinValue   float64 `json:"min_value"`
	MaxValue   float64 `json:"max_value"`
}

func (s *WorldService) UseItem(ctx context.Context, userID int64, req *pb.UseItemRequest) (int32, string) {
	player, code, msg := s.GetPlayerProfile(ctx, userID)
	if code != ErrCodeSuccess {
		return code, msg
	}

	if req.Quantity <= 0 {
		return ErrCodeInternal, "Số lượng không hợp lệ"
	}

	// 1. Get player inventory item
	inst, err := s.repo.GetPlayerItemInstance(ctx, player.ID, req.ItemId)
	if err != nil {
		return ErrCodeNotFound, "Vật phẩm không tồn tại trong túi đồ"
	}

	if inst.Quantity < req.Quantity {
		return ErrCodeNotEnough, "Không đủ số lượng vật phẩm"
	}

	// 2. Get item config
	cfg, err := s.repo.GetItemConfig(ctx, inst.ItemCode)
	if err != nil {
		return ErrCodeNotFound, "Cấu hình vật phẩm không tồn tại"
	}

	// 3. Parse effects
	var effects []ServiceItemEffect
	if cfg.Effects != "" && cfg.Effects != "[]" {
		if errJson := json.Unmarshal([]byte(cfg.Effects), &effects); errJson != nil {
			return ErrCodeInternal, "Lỗi phân tích hiệu ứng vật phẩm: " + errJson.Error()
		}
	}

	// 4. Apply logic per category
	useType := cfg.Type
	codeParam := ""
	valueParam := int32(0)

	switch useType {
	case "CURRENCY":
		// Find first currency effect
		for _, eff := range effects {
			if eff.EffectCode == "EFF_ADD_GOLD" {
				codeParam = "gold"
				valueParam = int32(eff.Value)
				break
			} else if eff.EffectCode == "EFF_ADD_DIAMOND" {
				codeParam = "diamond"
				valueParam = int32(eff.Value)
				break
			}
		}
		if codeParam == "" {
			return ErrCodeInternal, "Vật phẩm tiền tệ không cấu hình hiệu ứng hợp lệ"
		}
	case "SKIN_UNLOCKER":
		// Use item_code itself as the default skin code, or custom effect code param
		codeParam = cfg.ItemCode
		for _, eff := range effects {
			if eff.EffectCode != "" && eff.EffectCode != "EFF_UNLOCK_SKIN" {
				codeParam = eff.EffectCode
			}
		}
	case "VIP_LICENSE":
		// default VIP level 1, duration 30 days
		valueParam = 1
		codeParam = "30"
		for _, eff := range effects {
			if eff.EffectCode == "EFF_VIP_LICENSE" || eff.EffectCode == "EFF_ADD_VIP" {
				valueParam = int32(eff.Value)
				if eff.MaxValue > 0 {
					codeParam = fmt.Sprintf("%d", int32(eff.MaxValue))
				}
				break
			}
		}
	case "FUNCTION_UNLOCKER":
		// default function code is item_code
		codeParam = cfg.ItemCode
		for _, eff := range effects {
			if eff.EffectCode != "" && eff.EffectCode != "EFF_UNLOCK_FUNCTION" {
				codeParam = eff.EffectCode
			}
		}
	case "CONSUMABLE":
		// Check consumable stats: EFF_ADD_STAMINA, EFF_ADD_EXP etc.
		for _, eff := range effects {
			if eff.EffectCode == "EFF_ADD_STAMINA" {
				codeParam = "stamina"
				valueParam = int32(eff.Value)
				break
			} else if eff.EffectCode == "EFF_ADD_EXP" {
				codeParam = "exp"
				valueParam = int32(eff.Value)
				break
			}
		}
		if codeParam == "" {
			return ErrCodeInternal, "Vật phẩm tiêu hao không có hiệu ứng chỉ số hợp lệ"
		}
	default:
		return ErrCodeInternal, "Loại vật phẩm không hỗ trợ sử dụng trực tiếp: " + useType
	}

	// 5. Run DB Transaction
	err = s.repo.UseItemTransaction(ctx, player.ID, req.ItemId, req.Quantity, useType, codeParam, valueParam)
	if err != nil {
		return ErrCodeInternal, "Lỗi giao dịch sử dụng vật phẩm: " + err.Error()
	}

	return ErrCodeSuccess, "msg_use_item_success"
}

func (s *WorldService) GetMissions(ctx context.Context, userID int64, filterType pb.MissionType) ([]*pb.Mission, int32, string) {
	player, code, msg := s.GetPlayerProfile(ctx, userID)
	if code != ErrCodeSuccess {
		return nil, code, msg
	}

	// Fetch templates
	templates, err := s.repo.GetMissionTemplates(ctx)
	if err != nil {
		return nil, ErrCodeInternal, err.Error()
	}

	// Fetch player progress
	playerMissions, err := s.repo.GetPlayerMissions(ctx, player.ID)
	if err != nil {
		return nil, ErrCodeInternal, err.Error()
	}

	// Fetch player buildings for auto-evaluating build_upgrade missions
	buildings, _ := s.repo.GetPlayerBuildings(ctx, player.ID)
	buildingLevels := make(map[string]int32)
	for _, b := range buildings {
		buildingLevels[b.BuildingCode] = b.Level
	}

	progressMap := make(map[int32]*repository.DBPlayerMission)
	for _, pm := range playerMissions {
		progressMap[pm.MissionID] = pm
	}

	var result []*pb.Mission
	for _, t := range templates {
		if t.Type != int32(filterType) {
			continue
		}

		pm, exists := progressMap[t.MissionID]
		status := pb.MissionStatus_AVAILABLE
		currProgress := int32(0)

		if exists {
			status = pb.MissionStatus(pm.Status)
			currProgress = pm.CurrentProgress
		}

		// Auto evaluate player_level & build_upgrade if in available or in_progress status
		if status == pb.MissionStatus_AVAILABLE || status == pb.MissionStatus_IN_PROGRESS {
			if t.TargetType == "player_level" {
				currProgress = player.Level
			} else if t.TargetType == "build_upgrade" {
				if lvl, ok := buildingLevels[t.TargetParam]; ok {
					currProgress = lvl
				}
			}

			// Update state if completed
			if currProgress >= t.TargetProgress {
				status = pb.MissionStatus_COMPLETED
				currProgress = t.TargetProgress
			} else {
				status = pb.MissionStatus_IN_PROGRESS
			}

			if !exists {
				_ = s.repo.CreatePlayerMission(ctx, player.ID, t.MissionID, int32(status))
			}
			_ = s.repo.UpdatePlayerMissionProgress(ctx, player.ID, t.MissionID, currProgress, int32(status))
		}

		rewards := make(map[string]int32)
		if t.Rewards != "" && t.Rewards != "{}" {
			_ = json.Unmarshal([]byte(t.Rewards), &rewards)
		}

		result = append(result, &pb.Mission{
			MissionId:       t.MissionID,
			Title:           t.Title,
			Description:     t.Description,
			Type:            pb.MissionType(t.Type),
			Status:          status,
			CurrentProgress: currProgress,
			TargetProgress:  t.TargetProgress,
			Rewards:         rewards,
		})
	}

	return result, ErrCodeSuccess, ""
}

func (s *WorldService) ClaimMissionReward(ctx context.Context, userID int64, req *pb.ClaimMissionRewardRequest) (int32, string) {
	player, code, msg := s.GetPlayerProfile(ctx, userID)
	if code != ErrCodeSuccess {
		return code, msg
	}

	// Get player progress
	playerMissions, err := s.repo.GetPlayerMissions(ctx, player.ID)
	if err != nil {
		return ErrCodeInternal, err.Error()
	}

	var foundMission *repository.DBPlayerMission
	for _, pm := range playerMissions {
		if pm.MissionID == req.MissionId {
			foundMission = pm
			break
		}
	}

	if foundMission == nil {
		return ErrCodeNotFound, "Nhiệm vụ không tồn tại cho người chơi này"
	}

	if foundMission.Status != int32(pb.MissionStatus_COMPLETED) {
		return ErrCodeInternal, "Nhiệm vụ chưa hoàn thành hoặc đã nhận thưởng"
	}

	// Fetch templates
	templates, err := s.repo.GetMissionTemplates(ctx)
	if err != nil {
		return ErrCodeInternal, err.Error()
	}

	var matchedTemplate *repository.DBMissionTemplate
	for _, t := range templates {
		if t.MissionID == req.MissionId {
			matchedTemplate = t
			break
		}
	}

	if matchedTemplate == nil {
		return ErrCodeNotFound, "Cấu hình nhiệm vụ không tồn tại"
	}

	rewards := make(map[string]int32)
	if matchedTemplate.Rewards != "" && matchedTemplate.Rewards != "{}" {
		_ = json.Unmarshal([]byte(matchedTemplate.Rewards), &rewards)
	}

	err = s.repo.ClaimMissionRewardDB(ctx, player.ID, req.MissionId, rewards)
	if err != nil {
		return ErrCodeInternal, "Lỗi nhận thưởng: " + err.Error()
	}

	return ErrCodeSuccess, "msg_reward_claimed"
}

func (s *WorldService) TriggerMissionUpdate(ctx context.Context, playerID int64, targetType string, targetParam string, progressVal int32) {
	templates, err := s.repo.GetMissionTemplates(ctx)
	if err != nil {
		return
	}

	playerMissions, err := s.repo.GetPlayerMissions(ctx, playerID)
	if err != nil {
		return
	}

	progressMap := make(map[int32]*repository.DBPlayerMission)
	for _, pm := range playerMissions {
		progressMap[pm.MissionID] = pm
	}

	for _, t := range templates {
		if t.TargetType != targetType || (t.TargetParam != "" && t.TargetParam != targetParam) {
			continue
		}

		pm, exists := progressMap[t.MissionID]
		status := pb.MissionStatus_IN_PROGRESS
		currProgress := progressVal

		if exists {
			if pm.Status == int32(pb.MissionStatus_COMPLETED) || pm.Status == int32(pb.MissionStatus_REWARDED) {
				continue
			}
		} else {
			_ = s.repo.CreatePlayerMission(ctx, playerID, t.MissionID, int32(status))
		}

		if currProgress >= t.TargetProgress {
			status = pb.MissionStatus_COMPLETED
			currProgress = t.TargetProgress
		}

		_ = s.repo.UpdatePlayerMissionProgress(ctx, playerID, t.MissionID, currProgress, int32(status))
	}
}

func (s *WorldService) GetSkillConfigs(ctx context.Context) ([]*repository.SkillConfig, error) {
	return s.repo.GetSkillConfigs(ctx)
}

func (s *WorldService) LevelUpHero(ctx context.Context, userID int64, heroID int64) (*repository.PlayerHero, int32, string) {
	player, code, msg := s.GetPlayerProfile(ctx, userID)
	if code != ErrCodeSuccess {
		return nil, code, msg
	}

	hero, err := s.repo.LevelUpHero(ctx, player.ID, heroID)
	if err != nil {
		return nil, ErrCodeInternal, err.Error()
	}

	return hero, ErrCodeSuccess, ""
}

func (s *WorldService) ProcessPvECombatResult(ctx context.Context, userID int64, req *pb.ValidatePvEResultRequest) (*repository.Player, int32, string) {
	player, code, msg := s.GetPlayerProfile(ctx, userID)
	if code != ErrCodeSuccess {
		return nil, code, msg
	}

	// Default reward values if victory
	rewardExp := int32(100)
	rewardLinhThach := int32(50)

	updatedPlayer, err := s.repo.ProcessPvECombatResult(ctx, player.ID, req.EnemyId, req.IsVictory, rewardExp, rewardLinhThach)
	if err != nil {
		return nil, ErrCodeInternal, err.Error()
	}

	return updatedPlayer, ErrCodeSuccess, ""
}



