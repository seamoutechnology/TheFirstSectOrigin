package admin

import (
	"fmt"
	"strings"
)

type Service interface {
	GetZoneMeta() (MetaResponse, error)
	GetZoneData(tabID string, userID int64) (DataResponse, error)
	CreateAnnouncement(req AnnouncementRequest) error
	UpdateAnnouncement(req AnnouncementRequest) error
	DeleteAnnouncement(id int) error
	GetAllZones() ([]ZoneDB, error)
	GetActiveAnnouncements() ([]Announcement, error)
	GetAllAnnouncements() ([]Announcement, error)
	GetUserInfo(zoneID int, userID int64) (UserInfo, error)
	GetUsersPaginated(zoneID int, limit, offset int, search string) (UserListResponse, error)
	GetUserInventory(zoneID int, userID int64) ([]UserItem, error)
	AddUserItem(zoneID int, userID int64, itemCode string, quantity int) error
	RemoveUserItem(zoneID int, itemID int64) error

	GetAllItemConfigs() ([]ItemConfigData, error)
	SaveItemConfig(config ItemConfigData) error
	DeleteItemConfig(itemCode string) error

	GetAllEffectConfigs() ([]EffectConfigData, error)
	SaveEffectConfig(config EffectConfigData) error
	DeleteEffectConfig(effectCode string) error

	GetAllFeatureConfigs() ([]FeatureConfigData, error)
	SaveFeatureConfig(config FeatureConfigData) error
	DeleteFeatureConfig(featureCode string) error

	GetAllMissionTemplates() ([]MissionTemplateData, error)
	SaveMissionTemplate(config MissionTemplateData) error
	DeleteMissionTemplate(missionID int32) error
	GetAllStageConfigs() ([]StageConfigDB, error)
	SyncStageConfigs(req []SyncStageReq) error
	GetAllTraitConfigs() ([]TraitConfigDB, error)
	SyncTraitConfigs(req []SyncTraitReq) error
	GetAllHeroTemplates() ([]HeroTemplateDB, error)
	SyncHeroTemplates(req []HeroTemplateDB) error
	SyncBuildings(req []SyncBuildingReq) error
	GetPlayerInfoByUserID(userID int64, zoneID int) (PlayerInfoDB, error)
	RedeemGiftCode(zoneID int, playerID int64, code string) (string, error)
	RechargePlayer(zoneID int, playerID int64, amount int64) (int64, int64, error)
	GMAddHero(zoneID int, userID int64, heroCode string) error
	GMAddHeroWithTraits(zoneID int, userID int64, heroCode string, traits []string) error
	CreateGiftCode(code string, rewardGold int64, rewardDiamond int64, rewardItems string, maxUses int) error
}

type adminService struct {
	repo Repository
}

func NewService(repo Repository) Service {
	return &adminService{repo: repo}
}

func (s *adminService) GetZoneMeta() (MetaResponse, error) {
	count, _ := s.repo.GetActiveZonesCount()

	tabs := []TabInfo{
		{ID: "recent", Name: "Cụm Mới Nhất"},
		{ID: "my_chars", Name: "Đã Có Nhân Vật"},
	}

	const pageSize = 40
	numTabs := (count + pageSize - 1) / pageSize
	if numTabs == 0 {
		numTabs = 1
	}

	groupNames := []string{"Thanh Long", "Bạch Hổ", "Chu Tước", "Huyền Vũ", "Kỳ Lân", "Bạch Xà", "Hỏa Phượng"}

	for i := 0; i < numTabs; i++ {
		start := i*pageSize + 1
		end := (i + 1) * pageSize
		nameIdx := i % len(groupNames)
		tabName := fmt.Sprintf("Cụm %s (%d-%d)", groupNames[nameIdx], start, end)
		tabs = append(tabs, TabInfo{
			ID:   fmt.Sprintf("%d", i+1),
			Name: tabName,
		})
	}

	return MetaResponse{Tabs: tabs}, nil
}

func (s *adminService) GetAllZones() ([]ZoneDB, error) {
	return s.repo.GetAllZones()
}

func (s *adminService) GetZoneData(tabID string, userID int64) (DataResponse, error) {
	var dbZones []ZoneDB

	if tabID == "recent" {
		dbZones, _ = s.repo.GetRecentZones(20)
	} else if tabID == "my_chars" {
		dbZones, _ = s.repo.GetAllZones()
	} else {
		page := 1
		fmt.Sscanf(tabID, "%d", &page)
		if page < 1 {
			page = 1
		}
		limit := 40
		offset := (page - 1) * limit
		dbZones, _ = s.repo.GetZonesPaginated(limit, offset)
	}

	var allZones []ZoneResponse = make([]ZoneResponse, 0)

	for _, z := range dbZones {
		host := "localhost"
		port := 50052
		if parts := strings.SplitN(z.GatewayURL, ":", 2); len(parts) == 2 {
			host = parts[0]
			fmt.Sscanf(parts[1], "%d", &port)
		}

		hasCharacter := false
		charName := ""
		charLevel := 0

		if userID > 0 {
			player, err := s.repo.GetPlayerByUserID(userID, z.ID)
			if err == nil {
				hasCharacter = true
				charName = player.Nickname
				charLevel = player.Level
			}
		}

		if tabID == "my_chars" && !hasCharacter {
			continue
		}

		allZones = append(allZones, ZoneResponse{
			ID:              z.ID,
			Name:            z.Name,
			Host:            host,
			Port:            port,
			IsOnline:        z.Status != "maintenance",
			HasCharacter:    hasCharacter,
			CharacterName:   charName,
			CharacterLevel:  charLevel,
			CharacterAvatar: "avatar_01",
		})
	}

	return DataResponse{Zones: allZones}, nil
}

func (s *adminService) CreateAnnouncement(req AnnouncementRequest) error {
	return s.repo.CreateAnnouncement(req)
}

func (s *adminService) UpdateAnnouncement(req AnnouncementRequest) error {
	return s.repo.UpdateAnnouncement(req)
}

func (s *adminService) DeleteAnnouncement(id int) error {
	return s.repo.DeleteAnnouncement(id)
}

func (s *adminService) GetActiveAnnouncements() ([]Announcement, error) {
	return s.repo.GetActiveAnnouncements()
}

func (s *adminService) GetAllAnnouncements() ([]Announcement, error) {
	return s.repo.GetAllAnnouncements()
}


func (s *adminService) GetUserInfo(zoneID int, userID int64) (UserInfo, error) {
	return s.repo.GetUserInfo(zoneID, userID)
}

func (s *adminService) GetUsersPaginated(zoneID int, limit, offset int, search string) (UserListResponse, error) {
	return s.repo.GetUsersPaginated(zoneID, limit, offset, search)
}

func (s *adminService) GetUserInventory(zoneID int, userID int64) ([]UserItem, error) {
	return s.repo.GetUserInventory(zoneID, userID)
}

func (s *adminService) AddUserItem(zoneID int, userID int64, itemCode string, quantity int) error {
	return s.repo.AddUserItem(zoneID, userID, itemCode, quantity)
}

func (s *adminService) RemoveUserItem(zoneID int, itemID int64) error {
	return s.repo.RemoveUserItem(zoneID, itemID)
}


func (s *adminService) GetAllItemConfigs() ([]ItemConfigData, error) {
	return s.repo.GetAllItemConfigs()
}

func (s *adminService) SaveItemConfig(config ItemConfigData) error {
	return s.repo.SaveItemConfig(config)
}

func (s *adminService) DeleteItemConfig(itemCode string) error {
	return s.repo.DeleteItemConfig(itemCode)
}


func (s *adminService) GetAllEffectConfigs() ([]EffectConfigData, error) {
	return s.repo.GetAllEffectConfigs()
}

func (s *adminService) SaveEffectConfig(config EffectConfigData) error {
	return s.repo.SaveEffectConfig(config)
}

func (s *adminService) DeleteEffectConfig(effectCode string) error {
	return s.repo.DeleteEffectConfig(effectCode)
}

func (s *adminService) GetAllFeatureConfigs() ([]FeatureConfigData, error) {
	return s.repo.GetAllFeatureConfigs()
}

func (s *adminService) SaveFeatureConfig(config FeatureConfigData) error {
	return s.repo.SaveFeatureConfig(config)
}

func (s *adminService) DeleteFeatureConfig(featureCode string) error {
	return s.repo.DeleteFeatureConfig(featureCode)
}

func (s *adminService) GetAllMissionTemplates() ([]MissionTemplateData, error) {
	return s.repo.GetAllMissionTemplates()
}

func (s *adminService) SaveMissionTemplate(config MissionTemplateData) error {
	return s.repo.SaveMissionTemplate(config)
}

func (s *adminService) DeleteMissionTemplate(missionID int32) error {
	return s.repo.DeleteMissionTemplate(missionID)
}

func (s *adminService) SyncBuildings(req []SyncBuildingReq) error {
	return s.repo.SyncBuildings(req)
}

func (s *adminService) GetAllStageConfigs() ([]StageConfigDB, error) {
	return s.repo.GetAllStageConfigs()
}

func (s *adminService) SyncStageConfigs(req []SyncStageReq) error {
	return s.repo.SyncStageConfigs(req)
}

func (s *adminService) GetAllTraitConfigs() ([]TraitConfigDB, error) {
	return s.repo.GetAllTraitConfigs()
}

func (s *adminService) SyncTraitConfigs(req []SyncTraitReq) error {
	return s.repo.SyncTraitConfigs(req)
}

func (s *adminService) GetPlayerInfoByUserID(userID int64, zoneID int) (PlayerInfoDB, error) {
	return s.repo.GetPlayerInfoByUserID(userID, zoneID)
}

func (s *adminService) GetAllHeroTemplates() ([]HeroTemplateDB, error) {
	return s.repo.GetAllHeroTemplates()
}

func (s *adminService) SyncHeroTemplates(req []HeroTemplateDB) error {
	return s.repo.SyncHeroTemplates(req)
}

func (s *adminService) RedeemGiftCode(zoneID int, playerID int64, code string) (string, error) {
	return s.repo.RedeemGiftCode(zoneID, playerID, code)
}

func (s *adminService) RechargePlayer(zoneID int, playerID int64, amount int64) (int64, int64, error) {
	return s.repo.RechargePlayer(zoneID, playerID, amount)
}

func (s *adminService) GMAddHero(zoneID int, userID int64, heroCode string) error {
	return s.repo.GMAddHero(zoneID, userID, heroCode)
}

func (s *adminService) GMAddHeroWithTraits(zoneID int, userID int64, heroCode string, traits []string) error {
	return s.repo.GMAddHeroWithTraits(zoneID, userID, heroCode, traits)
}

func (s *adminService) CreateGiftCode(code string, rewardGold int64, rewardDiamond int64, rewardItems string, maxUses int) error {
	return s.repo.CreateGiftCode(code, rewardGold, rewardDiamond, rewardItems, maxUses)
}

