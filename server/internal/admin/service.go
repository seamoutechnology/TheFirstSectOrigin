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
	GetUserInfo(userID int64) (UserInfo, error)
	GetUsersPaginated(limit, offset int, search string) (UserListResponse, error)
	GetUserInventory(zoneID int, userID int64) ([]UserItem, error)
	AddUserItem(zoneID int, userID int64, itemCode string, quantity int) error
	RemoveUserItem(zoneID int, itemID int64) error

	GetAllItemConfigs() ([]ItemConfigData, error)
	SaveItemConfig(config ItemConfigData) error
	DeleteItemConfig(itemCode string) error

	GetAllEffectConfigs() ([]EffectConfigData, error)
	SaveEffectConfig(config EffectConfigData) error
	DeleteEffectConfig(effectCode string) error
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


func (s *adminService) GetUserInfo(userID int64) (UserInfo, error) {
	return s.repo.GetUserInfo(userID)
}

func (s *adminService) GetUsersPaginated(limit, offset int, search string) (UserListResponse, error) {
	return s.repo.GetUsersPaginated(limit, offset, search)
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
