package admin

import (
	"encoding/json"
	"fmt"
	"sync"
	"server/pkg/config"

	"github.com/jmoiron/sqlx"
)

type Repository interface {
	GetActiveZonesCount() (int, error)
	GetRecentZones(limit int) ([]ZoneDB, error)
	GetAllZones() ([]ZoneDB, error)
	GetZonesPaginated(limit, offset int) ([]ZoneDB, error)
	GetPlayerByUserID(userID int64, zoneID int) (PlayerDB, error)
	CreateAnnouncement(req AnnouncementRequest) error
	UpdateAnnouncement(req AnnouncementRequest) error
	DeleteAnnouncement(id int) error
	GetActiveAnnouncements() ([]Announcement, error)
	GetAllAnnouncements() ([]Announcement, error)
	GetUserInfo(zoneID int, userID int64) (UserInfo, error)
	GetUsersPaginated(zoneID int, limit, offset int, search string) (UserListResponse, error)
	GetUserInventory(zoneID int, userID int64) ([]UserItem, error)
	AddUserItem(zoneID int, userID int64, itemCode string, quantity int) error
	RemoveUserItem(zoneID int, itemID int64) error
	CleanupOrphanedPlayers(zoneID int) (int64, error)

	GetAllItemConfigs() ([]ItemConfigData, error)
	SaveItemConfig(config ItemConfigData) error
	DeleteItemConfig(itemCode string) error

	GetAllShopItems() ([]ShopItemData, error)
	SaveShopItem(config ShopItemData) error
	DeleteShopItem(shopItemID string) error

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

type adminRepo struct {
	db      *sqlx.DB
	gameDBs map[int]*sqlx.DB
	mu      sync.RWMutex
	cfg     *config.Config
}

func NewRepository(db *sqlx.DB, defaultGameDB *sqlx.DB, cfg *config.Config) Repository {
	gameDBs := make(map[int]*sqlx.DB)
	gameDBs[1] = defaultGameDB // Mặc định zone 1
	return &adminRepo{db: db, gameDBs: gameDBs, cfg: cfg}
}

func (r *adminRepo) getGameDB(zoneID int) (*sqlx.DB, error) {
	r.mu.RLock()
	db, exists := r.gameDBs[1]
	r.mu.RUnlock()
	if exists {
		return db, nil
	}
	
	r.mu.Lock()
	defer r.mu.Unlock()
	if db, exists := r.gameDBs[1]; exists {
		return db, nil
	}
	
	dsn := r.cfg.GameDB.DSN
	newDB, err := sqlx.Connect("postgres", dsn)
	if err != nil {
		return nil, fmt.Errorf("không thể kết nối Game DB mặc định: %v", err)
	}
	
	r.gameDBs[1] = newDB
	return newDB, nil
}

type ZoneDB struct {
	ID         int    `db:"id" json:"id"`
	Name       string `db:"name" json:"name"`
	GatewayURL string `db:"gateway_url" json:"gateway_url"`
	Status     string `db:"status" json:"status"`
	IsActive   bool   `db:"is_active" json:"is_active"`
}

type PlayerDB struct {
	Nickname string `db:"nickname"`
	Level    int    `db:"level"`
}

func (r *adminRepo) GetActiveZonesCount() (int, error) {
	var count int
	err := r.db.Get(&count, "SELECT COUNT(*) FROM zones WHERE is_active = true")
	return count, err
}

func (r *adminRepo) GetRecentZones(limit int) ([]ZoneDB, error) {
	var zones []ZoneDB
	err := r.db.Select(&zones, "SELECT id, name, gateway_url, status, is_active FROM zones WHERE is_active = true ORDER BY id DESC LIMIT $1", limit)
	return zones, err
}

func (r *adminRepo) GetAllZones() ([]ZoneDB, error) {
	var zones []ZoneDB
	err := r.db.Select(&zones, "SELECT id, name, gateway_url, status, is_active FROM zones WHERE is_active = true ORDER BY id ASC")
	return zones, err
}

func (r *adminRepo) GetZonesPaginated(limit, offset int) ([]ZoneDB, error) {
	var zones []ZoneDB
	err := r.db.Select(&zones, "SELECT id, name, gateway_url, status, is_active FROM zones WHERE is_active = true ORDER BY id ASC LIMIT $1 OFFSET $2", limit, offset)
	return zones, err
}

func (r *adminRepo) GetPlayerByUserID(userID int64, zoneID int) (PlayerDB, error) {
	var player PlayerDB
	db, err := r.getGameDB(1)
	if err != nil {
		return player, err
	}
	serverID := fmt.Sprintf("zone%d", zoneID)
	err = db.Get(&player, "SELECT nickname, level FROM players WHERE user_id = $1 AND server_id = $2", userID, serverID)
	return player, err
}

func (r *adminRepo) CreateAnnouncement(req AnnouncementRequest) error {
	query := `INSERT INTO announcements (type, title, content, start_at, end_at, is_active) VALUES ($1, $2, $3, $4, $5, $6)`
	_, err := r.db.Exec(query, req.Type, req.Title, req.Content, req.StartAt, req.EndAt, req.IsActive)
	return err
}

func (r *adminRepo) UpdateAnnouncement(req AnnouncementRequest) error {
	query := `UPDATE announcements SET type=$1, title=$2, content=$3, start_at=$4, end_at=$5, is_active=$6 WHERE id=$7`
	_, err := r.db.Exec(query, req.Type, req.Title, req.Content, req.StartAt, req.EndAt, req.IsActive, req.ID)
	return err
}

func (r *adminRepo) DeleteAnnouncement(id int) error {
	query := `DELETE FROM announcements WHERE id=$1`
	_, err := r.db.Exec(query, id)
	return err
}

func (r *adminRepo) GetActiveAnnouncements() ([]Announcement, error) {
	var list []Announcement
	query := `SELECT * FROM announcements WHERE is_active = true AND CURRENT_TIMESTAMP BETWEEN start_at AND end_at ORDER BY created_at DESC`
	err := r.db.Select(&list, query)
	return list, err
}

func (r *adminRepo) GetAllAnnouncements() ([]Announcement, error) {
	var list []Announcement
	query := `SELECT * FROM announcements ORDER BY id DESC`
	err := r.db.Select(&list, query)
	return list, err
}


func (r *adminRepo) GetUserInfo(zoneID int, userID int64) (UserInfo, error) {
	var info UserInfo
	info.UserID = userID
	info.Disciples = make([]UserHeroInfo, 0)
	_ = r.db.Get(&info.Email, "SELECT email FROM users WHERE id = $1", userID)
	
	db, err := r.getGameDB(zoneID)
	if err != nil {
		return info, err
	}
	serverID := fmt.Sprintf("zone%d", zoneID)
	var playerID int64
	err = db.QueryRow("SELECT id, nickname, level, gold FROM players WHERE user_id = $1 AND server_id = $2", userID, serverID).Scan(&playerID, &info.SectName, &info.Level, &info.Money)
	if err != nil {
		// Return the info as is, but log or handle the error
		return info, nil
	}
	
	rows, err := db.Query(`
		SELECT ph.id, ph.hero_code, ht.name, ht.rarity, ph.level, ph.star
		FROM player_heroes ph
		JOIN hero_templates ht ON ht.code = ph.hero_code
		WHERE ph.player_id = $1
		ORDER BY ph.level DESC, ht.rarity DESC
	`, playerID)
	if err == nil {
		defer rows.Close()
		for rows.Next() {
			var h UserHeroInfo
			if err := rows.Scan(&h.ID, &h.HeroCode, &h.Name, &h.Rarity, &h.Level, &h.Star); err == nil {
				info.Disciples = append(info.Disciples, h)
			}
		}
	}
	
	return info, nil
}

func (r *adminRepo) GetUsersPaginated(zoneID int, limit, offset int, search string) (UserListResponse, error) {
	var resp UserListResponse
	resp.Data = make([]UserListItem, 0)

	countQuery := "SELECT COUNT(*) FROM users"
	argsCount := []interface{}{}
	if search != "" {
		countQuery += " WHERE email ILIKE $1 OR username ILIKE $1"
		argsCount = append(argsCount, "%"+search+"%")
	}
	err := r.db.Get(&resp.Total, countQuery, argsCount...)
	if err != nil {
		return resp, err
	}

	dataQuery := "SELECT id, email FROM users"
	argsData := []interface{}{}
	if search != "" {
		dataQuery += " WHERE email ILIKE $1 OR username ILIKE $1"
		argsData = append(argsData, "%"+search+"%")
	}
	dataQuery += " ORDER BY id DESC LIMIT $" + fmt.Sprintf("%d", len(argsData)+1) + " OFFSET $" + fmt.Sprintf("%d", len(argsData)+2)
	argsData = append(argsData, limit, offset)

	var users []struct {
		ID    int64  `db:"id"`
		Email string `db:"email"`
	}
	err = r.db.Select(&users, dataQuery, argsData...)
	if err != nil {
		return resp, err
	}

	if len(users) == 0 {
		return resp, nil
	}

	var userIDs []int64
	for _, u := range users {
		userIDs = append(userIDs, u.ID)
	}
	
	serverID := fmt.Sprintf("zone%d", zoneID)
	query, args, err := sqlx.In("SELECT user_id, nickname, level FROM players WHERE user_id IN (?) AND server_id = ?", userIDs, serverID)
	if err != nil {
		return resp, err
	}
	db, err := r.getGameDB(zoneID)
	if err != nil {
		return resp, err
	}
	query = db.Rebind(query)

	var players []struct {
		UserID   int64  `db:"user_id"`
		Nickname string `db:"nickname"`
		Level    int    `db:"level"`
	}
	_ = db.Select(&players, query, args...)

	playerMap := make(map[int64]struct {
		Nickname string
		Level    int
	})
	for _, p := range players {
		playerMap[p.UserID] = struct {
			Nickname string
			Level    int
		}{p.Nickname, p.Level}
	}

	for _, u := range users {
		pm := playerMap[u.ID]
		resp.Data = append(resp.Data, UserListItem{
			UserID:   u.ID,
			Email:    u.Email,
			Nickname: pm.Nickname,
			Level:    pm.Level,
		})
	}

	return resp, nil
}


func (r *adminRepo) GetUserInventory(zoneID int, userID int64) ([]UserItem, error) {
	db, err := r.getGameDB(zoneID)
	if err != nil {
		return nil, err
	}
	var playerID int64
	serverID := fmt.Sprintf("zone%d", zoneID)
	err = db.Get(&playerID, "SELECT id FROM players WHERE user_id = $1 AND server_id = $2", userID, serverID)
	if err != nil {
		return nil, fmt.Errorf("không tìm thấy nhân vật ở zone %d: %v", zoneID, err)
	}

	var items []UserItem
	err = db.Select(&items, "SELECT id, player_id, item_code, quantity FROM user_items WHERE player_id = $1 ORDER BY id DESC", playerID)
	return items, err
}

func (r *adminRepo) AddUserItem(zoneID int, userID int64, itemCode string, quantity int) error {
	db, err := r.getGameDB(zoneID)
	if err != nil {
		return err
	}
	var playerID int64
	serverID := fmt.Sprintf("zone%d", zoneID)
	err = db.Get(&playerID, "SELECT id FROM players WHERE user_id = $1 AND server_id = $2", userID, serverID)
	if err != nil {
		return fmt.Errorf("không tìm thấy nhân vật ở zone %d: %v", zoneID, err)
	}

	var id int64
	err = db.QueryRow("SELECT id FROM user_items WHERE player_id = $1 AND item_code = $2 LIMIT 1", playerID, itemCode).Scan(&id)
	
	if err == nil {
		_, err = db.Exec("UPDATE user_items SET quantity = quantity + $1 WHERE id = $2", quantity, id)
	} else {
	    _, err = db.Exec("INSERT INTO user_items (player_id, item_code, quantity) VALUES ($1, $2, $3)", playerID, itemCode, quantity)
    }

	// Đồng bộ cập nhật tiền tệ trực tiếp vào bảng players
	if itemCode == "00001" || itemCode == "gold" {
		db.Exec("UPDATE players SET gold = gold + $1 WHERE id = $2", quantity, playerID)
	} else if itemCode == "00000" || itemCode == "diamond" || itemCode == "qi" {
		db.Exec("UPDATE players SET diamond = diamond + $1 WHERE id = $2", quantity, playerID)
	} else if itemCode == "stamina" {
		db.Exec("UPDATE players SET stamina = stamina + $1 WHERE id = $2", quantity, playerID)
	}

	return err
}

func (r *adminRepo) RemoveUserItem(zoneID int, itemID int64) error {
	db, err := r.getGameDB(zoneID)
	if err != nil {
		return err
	}
	_, err = db.Exec("DELETE FROM user_items WHERE id = $1", itemID)
	return err
}


func (r *adminRepo) GetAllItemConfigs() ([]ItemConfigData, error) {
	var list []ItemConfigData
	query := "SELECT item_code, name_key, type, rarity, icon, desc_key, max_stack, sources, effects, required_level FROM item_configs ORDER BY item_code ASC"
	db, _ := r.getGameDB(1)
	err := db.Select(&list, query)
	return list, err
}

func (r *adminRepo) SaveItemConfig(c ItemConfigData) error {
	if c.Sources == "" {
		c.Sources = "[]"
	}
	if c.Effects == "" {
		c.Effects = "[]"
	}
	query := `
		INSERT INTO item_configs (item_code, name_key, type, rarity, icon, desc_key, max_stack, sources, effects, required_level)
		VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10)
		ON CONFLICT (item_code) DO UPDATE SET
			name_key = EXCLUDED.name_key,
			type = EXCLUDED.type,
			rarity = EXCLUDED.rarity,
			icon = EXCLUDED.icon,
			desc_key = EXCLUDED.desc_key,
			max_stack = EXCLUDED.max_stack,
			sources = EXCLUDED.sources,
			effects = EXCLUDED.effects,
			required_level = EXCLUDED.required_level,
			updated_at = CURRENT_TIMESTAMP
	`
	db, _ := r.getGameDB(1)
	_, err := db.Exec(query, c.ItemCode, c.NameKey, c.Type, c.Rarity, c.Icon, c.DescKey, c.MaxStack, c.Sources, c.Effects, c.RequiredLevel)
	return err
}

func (r *adminRepo) DeleteItemConfig(itemCode string) error {
	db, _ := r.getGameDB(1)
	_, err := db.Exec("DELETE FROM item_configs WHERE item_code = $1", itemCode)
	return err
}


func (r *adminRepo) GetAllEffectConfigs() ([]EffectConfigData, error) {
	var list []EffectConfigData
	query := "SELECT effect_code, name_key, desc_key, icon, effect_type, value_type, min_value, max_value, source_stat FROM effect_configs ORDER BY effect_code ASC"
	db, _ := r.getGameDB(1)
	err := db.Select(&list, query)
	return list, err
}

func (r *adminRepo) SaveEffectConfig(c EffectConfigData) error {
	query := `
		INSERT INTO effect_configs (effect_code, name_key, desc_key, icon, effect_type, value_type, min_value, max_value, source_stat)
		VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)
		ON CONFLICT (effect_code) DO UPDATE SET
			name_key = EXCLUDED.name_key,
			desc_key = EXCLUDED.desc_key,
			icon = EXCLUDED.icon,
			effect_type = EXCLUDED.effect_type,
			value_type = EXCLUDED.value_type,
			min_value = EXCLUDED.min_value,
			max_value = EXCLUDED.max_value,
			source_stat = EXCLUDED.source_stat,
			updated_at = CURRENT_TIMESTAMP
	`
	db, _ := r.getGameDB(1)
	_, err := db.Exec(query, c.EffectCode, c.NameKey, c.DescKey, c.Icon, c.EffectType, c.ValueType, c.MinValue, c.MaxValue, c.SourceStat)
	return err
}

func (r *adminRepo) DeleteEffectConfig(effectCode string) error {
	db, _ := r.getGameDB(1)
	_, err := db.Exec("DELETE FROM effect_configs WHERE effect_code = $1", effectCode)
	return err
}

func (r *adminRepo) GetAllFeatureConfigs() ([]FeatureConfigData, error) {
	var list []FeatureConfigData
	query := "SELECT feature_code, name_key, icon, required_player_level, required_mission_code, is_active FROM feature_configs ORDER BY feature_code ASC"
	db, _ := r.getGameDB(1)
	err := db.Select(&list, query)
	return list, err
}

func (r *adminRepo) SaveFeatureConfig(c FeatureConfigData) error {
	query := `
		INSERT INTO feature_configs (feature_code, name_key, icon, required_player_level, required_mission_code, is_active)
		VALUES ($1, $2, $3, $4, $5, $6)
		ON CONFLICT (feature_code) DO UPDATE SET
			name_key = EXCLUDED.name_key,
			icon = EXCLUDED.icon,
			required_player_level = EXCLUDED.required_player_level,
			required_mission_code = EXCLUDED.required_mission_code,
			is_active = EXCLUDED.is_active,
			updated_at = CURRENT_TIMESTAMP
	`
	db, _ := r.getGameDB(1)
	_, err := db.Exec(query, c.FeatureCode, c.NameKey, c.Icon, c.RequiredPlayerLevel, c.RequiredMissionCode, c.IsActive)
	return err
}

func (r *adminRepo) DeleteFeatureConfig(featureCode string) error {
	db, _ := r.getGameDB(1)
	_, err := db.Exec("DELETE FROM feature_configs WHERE feature_code = $1", featureCode)
	return err
}

func (r *adminRepo) GetAllMissionTemplates() ([]MissionTemplateData, error) {
	var list []MissionTemplateData
	query := "SELECT mission_id, title, description, type, target_type, target_param, target_progress, rewards FROM mission_templates ORDER BY mission_id ASC"
	db, _ := r.getGameDB(1)
	err := db.Select(&list, query)
	return list, err
}

func (r *adminRepo) SaveMissionTemplate(c MissionTemplateData) error {
	if c.Rewards == "" {
		c.Rewards = "{}"
	}
	query := `
		INSERT INTO mission_templates (mission_id, title, description, type, target_type, target_param, target_progress, rewards)
		VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
		ON CONFLICT (mission_id) DO UPDATE SET
			title = EXCLUDED.title,
			description = EXCLUDED.description,
			type = EXCLUDED.type,
			target_type = EXCLUDED.target_type,
			target_param = EXCLUDED.target_param,
			target_progress = EXCLUDED.target_progress,
			rewards = EXCLUDED.rewards,
			updated_at = CURRENT_TIMESTAMP
	`
	db, _ := r.getGameDB(1)
	_, err := db.Exec(query, c.MissionID, c.Title, c.Description, c.Type, c.TargetType, c.TargetParam, c.TargetProgress, c.Rewards)
	return err
}

func (r *adminRepo) DeleteMissionTemplate(missionID int32) error {
	db, _ := r.getGameDB(1)
	_, err := db.Exec("DELETE FROM mission_templates WHERE mission_id = $1", missionID)
	return err
}

func (r *adminRepo) SyncBuildings(req []SyncBuildingReq) error {
	db, err := r.getGameDB(1)
	if err != nil {
		return err
	}

	tx, err := db.Beginx()
	if err != nil {
		return err
	}
	defer tx.Rollback()

	// Thu thập danh sách các code đang hoạt động để làm whitelist
	activeCodes := make([]string, 0, len(req))
	for _, b := range req {
		activeCodes = append(activeCodes, b.Code)
	}

	// 1. Xóa các công trình không còn tồn tại trong config khỏi bảng player_buildings trước (do khóa ngoại)
	if len(activeCodes) > 0 {
		queryDeletePlayerBuildings := `
			DELETE FROM player_buildings 
			WHERE building_code NOT IN (SELECT unnest($1::text[]))
		`
		_, err = tx.Exec(queryDeletePlayerBuildings, activeCodes)
		if err != nil {
			return fmt.Errorf("failed to delete removed buildings from player_buildings: %v", err)
		}

		// 2. Xóa khỏi bảng buildings
		queryDeleteBuildings := `
			DELETE FROM buildings 
			WHERE code NOT IN (SELECT unnest($1::text[]))
		`
		_, err = tx.Exec(queryDeleteBuildings, activeCodes)
		if err != nil {
			return fmt.Errorf("failed to delete removed buildings from buildings table: %v", err)
		}
	} else {
		// Nếu xuất mảng rỗng thì xóa toàn bộ
		_, err = tx.Exec("DELETE FROM player_buildings")
		if err != nil {
			return err
		}
		_, err = tx.Exec("DELETE FROM buildings")
		if err != nil {
			return err
		}
	}

	// 3. Insert/update các công trình hợp lệ và init cho người chơi
	for _, b := range req {
		queryBuilding := `
			INSERT INTO buildings (code, name, max_level, description)
			VALUES ($1, $2, $3, $4)
			ON CONFLICT (code) DO UPDATE SET
				name = EXCLUDED.name,
				max_level = EXCLUDED.max_level,
				description = EXCLUDED.description
		`
		_, err = tx.Exec(queryBuilding, b.Code, b.Name, b.MaxLevel, b.Desc)
		if err != nil {
			return fmt.Errorf("failed to sync building %s: %v", b.Code, err)
		}

		queryPlayerBuilding := `
			INSERT INTO player_buildings (player_id, building_code, level)
			SELECT id, $1, 1 FROM players
			ON CONFLICT (player_id, building_code) DO NOTHING
		`
		_, err = tx.Exec(queryPlayerBuilding, b.Code)
		if err != nil {
			return fmt.Errorf("failed to init player building for %s: %v", b.Code, err)
		}
	}

	return tx.Commit()
}

func (r *adminRepo) GetAllStageConfigs() ([]StageConfigDB, error) {
	db, err := r.getGameDB(1)
	if err != nil {
		return nil, err
	}

	var stages []StageConfigDB
	err = db.Select(&stages, "SELECT stage_id, json_data, updated_at FROM stage_configs")
	if err != nil {
		return nil, err
	}
	return stages, nil
}

func (r *adminRepo) SyncStageConfigs(req []SyncStageReq) error {
	db, err := r.getGameDB(1)
	if err != nil {
		return err
	}

	tx, err := db.Beginx()
	if err != nil {
		return err
	}
	defer tx.Rollback()

	_, err = tx.Exec("DELETE FROM stage_configs")
	if err != nil {
		return err
	}

	for _, s := range req {
		query := `
			INSERT INTO stage_configs (stage_id, json_data, updated_at)
			VALUES ($1, $2, CURRENT_TIMESTAMP)
		`
		_, err = tx.Exec(query, s.StageID, s.JSONData)
		if err != nil {
			return fmt.Errorf("failed to sync stage %s: %v", s.StageID, err)
		}
	}

	return tx.Commit()
}

func (r *adminRepo) GetAllTraitConfigs() ([]TraitConfigDB, error) {
	db, err := r.getGameDB(1)
	if err != nil {
		return nil, err
	}

	var traits []TraitConfigDB
	err = db.Select(&traits, "SELECT trait_code, weight, json_data, updated_at FROM trait_configs")
	if err != nil {
		return nil, err
	}
	return traits, nil
}

func (r *adminRepo) SyncTraitConfigs(req []SyncTraitReq) error {
	db, err := r.getGameDB(1)
	if err != nil {
		return err
	}

	tx, err := db.Beginx()
	if err != nil {
		return err
	}
	defer tx.Rollback()

	_, err = tx.Exec("DELETE FROM trait_configs")
	if err != nil {
		return err
	}

	for _, t := range req {
		query := `
			INSERT INTO trait_configs (trait_code, weight, json_data, updated_at)
			VALUES ($1, $2, $3, CURRENT_TIMESTAMP)
		`
		_, err = tx.Exec(query, t.TraitCode, t.Weight, t.JSONData)
		if err != nil {
			return fmt.Errorf("failed to sync trait %s: %v", t.TraitCode, err)
		}
	}

	return tx.Commit()
}

type PlayerInfoDB struct {
	ID       int64  `db:"id"`
	Nickname string `db:"nickname"`
	Level    int    `db:"level"`
	Gold     int64  `db:"gold"`
	Diamond  int64  `db:"diamond"`
}

func (r *adminRepo) GetPlayerInfoByUserID(userID int64, zoneID int) (PlayerInfoDB, error) {
	var player PlayerInfoDB
	db, err := r.getGameDB(zoneID)
	if err != nil {
		return player, err
	}
	serverID := fmt.Sprintf("zone%d", zoneID)
	// Lấy người chơi đầu tiên thuộc về UserID này và server_id trong Database game
	err = db.Get(&player, "SELECT id, nickname, level, gold, diamond FROM players WHERE user_id = $1 AND server_id = $2 LIMIT 1", userID, serverID)
	if err != nil {
		return player, err
	}

	// Đồng bộ thông số hiển thị từ user_items (nếu có) để khớp với Client
	var itemGold int64
	err = db.Get(&itemGold, "SELECT quantity FROM user_items WHERE player_id = $1 AND item_code = '00001'", player.ID)
	if err == nil {
		player.Gold = itemGold
	}

	var itemDiamond int64
	err = db.Get(&itemDiamond, "SELECT quantity FROM user_items WHERE player_id = $1 AND item_code = '00000'", player.ID)
	if err == nil {
		player.Diamond = itemDiamond
	}

	return player, nil
}

func (r *adminRepo) RedeemGiftCode(zoneID int, playerID int64, code string) (string, error) {
	db, err := r.getGameDB(zoneID)
	if err != nil {
		return "", err
	}

	tx, err := db.Beginx()
	if err != nil {
		return "", err
	}
	defer tx.Rollback()

	// 1. Khóa và kiểm tra gift code
	var giftCode struct {
		Code           string `db:"code"`
		RewardGold     int64  `db:"reward_gold"`
		RewardDiamond  int64  `db:"reward_diamond"`
		RewardItems    string `db:"reward_items"`
		MaxUses        int    `db:"max_uses"`
		UsedCount      int    `db:"used_count"`
	}
	err = tx.Get(&giftCode, "SELECT code, reward_gold, reward_diamond, reward_items, max_uses, used_count FROM gift_codes WHERE UPPER(code) = UPPER($1) FOR UPDATE", code)
	if err != nil {
		return "", fmt.Errorf("Mã quà tặng không hợp lệ hoặc không tồn tại")
	}

	if giftCode.UsedCount >= giftCode.MaxUses {
		return "", fmt.Errorf("Mã quà tặng đã đạt giới hạn lượt sử dụng")
	}

	// 2. Kiểm tra xem người chơi đã sử dụng mã này chưa
	var usageCount int
	err = tx.Get(&usageCount, "SELECT COUNT(*) FROM gift_code_usages WHERE player_id = $1 AND code = $2", playerID, giftCode.Code)
	if err != nil {
		return "", err
	}
	if usageCount > 0 {
		return "", fmt.Errorf("Bạn đã sử dụng mã quà tặng này rồi")
	}

	// 3. Ghi nhận lượt sử dụng
	_, err = tx.Exec("INSERT INTO gift_code_usages (player_id, code) VALUES ($1, $2)", playerID, giftCode.Code)
	if err != nil {
		return "", err
	}

	// 4. Cập nhật số lượng sử dụng của mã
	_, err = tx.Exec("UPDATE gift_codes SET used_count = used_count + 1 WHERE code = $1", giftCode.Code)
	if err != nil {
		return "", err
	}

	// 5. Cộng tài nguyên (Vàng, Qi/Kim Cương) cho người chơi bằng Item
	if giftCode.RewardGold > 0 || giftCode.RewardDiamond > 0 {

		if giftCode.RewardGold > 0 {
			var hasGoldItem bool
			err = tx.Get(&hasGoldItem, "SELECT EXISTS(SELECT 1 FROM user_items WHERE player_id = $1 AND item_code = '00001')", playerID)
			if err != nil {
				return "", err
			}
			if hasGoldItem {
				_, err = tx.Exec("UPDATE user_items SET quantity = quantity + $1, updated_at = NOW() WHERE player_id = $2 AND item_code = '00001'", giftCode.RewardGold, playerID)
			} else {
				_, err = tx.Exec("INSERT INTO user_items (player_id, item_code, quantity, stats) VALUES ($1, '00001', $2, '[]')", playerID, giftCode.RewardGold)
			}
			if err != nil {
				return "", err
			}
		}

		if giftCode.RewardDiamond > 0 {
			var hasXuItem bool
			err = tx.Get(&hasXuItem, "SELECT EXISTS(SELECT 1 FROM user_items WHERE player_id = $1 AND item_code = '00000')", playerID)
			if err != nil {
				return "", err
			}
			if hasXuItem {
				_, err = tx.Exec("UPDATE user_items SET quantity = quantity + $1, updated_at = NOW() WHERE player_id = $2 AND item_code = '00000'", giftCode.RewardDiamond, playerID)
			} else {
				_, err = tx.Exec("INSERT INTO user_items (player_id, item_code, quantity, stats) VALUES ($1, '00000', $2, '[]')", playerID, giftCode.RewardDiamond)
			}
			if err != nil {
				return "", err
			}
		}
	}

	// 6. Tặng vật phẩm trong túi đồ
	var items []struct {
		ItemCode string `json:"item_code"`
		Quantity int    `json:"quantity"`
	}
	if giftCode.RewardItems != "" && giftCode.RewardItems != "[]" {
		err = json.Unmarshal([]byte(giftCode.RewardItems), &items)
		if err == nil {
			for _, item := range items {
				var currentQty int
				err = tx.Get(&currentQty, "SELECT quantity FROM user_items WHERE player_id = $1 AND item_code = $2", playerID, item.ItemCode)
				if err == nil {
					_, err = tx.Exec("UPDATE user_items SET quantity = quantity + $1, updated_at = NOW() WHERE player_id = $2 AND item_code = $3", item.Quantity, playerID, item.ItemCode)
				} else {
					_, err = tx.Exec("INSERT INTO user_items (player_id, item_code, quantity, stats) VALUES ($1, $2, $3, '{}')", playerID, item.ItemCode, item.Quantity)
				}
				if err != nil {
					return "", err
				}
			}
		}
	}

	err = tx.Commit()
	if err != nil {
		return "", err
	}

	desc := ""
	if giftCode.RewardGold > 0 {
		desc += fmt.Sprintf("%d Vàng, ", giftCode.RewardGold)
	}
	if giftCode.RewardDiamond > 0 {
		desc += fmt.Sprintf("%d Xu, ", giftCode.RewardDiamond)
	}
	if len(items) > 0 {
		desc += fmt.Sprintf("%d vật phẩm túi đồ, ", len(items))
	}
	if len(desc) > 2 {
		desc = desc[:len(desc)-2]
	}
	return desc, nil
}

func (r *adminRepo) RechargePlayer(zoneID int, playerID int64, amount int64) (int64, int64, error) {
	db, err := r.getGameDB(zoneID)
	if err != nil {
		return 0, 0, err
	}

	tx, err := db.Beginx()
	if err != nil {
		return 0, 0, err
	}
	defer tx.Rollback()

	// 1000đ nạp = 10 Gold + 1 Diamond
	diamondReward := amount / 1000
	goldReward := amount * 10

	// 1. Lưu lịch sử nạp
	_, err = tx.Exec("INSERT INTO recharge_history (player_id, amount, diamond_reward, gold_reward, status) VALUES ($1, $2, $3, $4, 'success')", playerID, amount, diamondReward, goldReward)
	if err != nil {
		return 0, 0, err
	}

	// 2. Không cộng tiền vào bảng players nữa (đã chuyển sang Item)

	// Đồng bộ vào user_items
	// Vàng thỏi: 00001
	var hasGoldItem bool
	err = tx.Get(&hasGoldItem, "SELECT EXISTS(SELECT 1 FROM user_items WHERE player_id = $1 AND item_code = '00001')", playerID)
	if err != nil {
		return 0, 0, err
	}
	if hasGoldItem {
		_, err = tx.Exec("UPDATE user_items SET quantity = quantity + $1, updated_at = NOW() WHERE player_id = $2 AND item_code = '00001'", goldReward, playerID)
	} else {
		_, err = tx.Exec("INSERT INTO user_items (player_id, item_code, quantity, stats) VALUES ($1, '00001', $2, '[]')", playerID, goldReward)
	}
	if err != nil {
		return 0, 0, err
	}

	// Xu/Diamond: 00000
	var hasXuItem bool
	err = tx.Get(&hasXuItem, "SELECT EXISTS(SELECT 1 FROM user_items WHERE player_id = $1 AND item_code = '00000')", playerID)
	if err != nil {
		return 0, 0, err
	}
	if hasXuItem {
		_, err = tx.Exec("UPDATE user_items SET quantity = quantity + $1, updated_at = NOW() WHERE player_id = $2 AND item_code = '00000'", diamondReward, playerID)
	} else {
		_, err = tx.Exec("INSERT INTO user_items (player_id, item_code, quantity, stats) VALUES ($1, '00000', $2, '[]')", playerID, diamondReward)
	}
	if err != nil {
		return 0, 0, err
	}

	// 3. Lấy chỉ số mới từ túi đồ
	var newGold, newDiamond int64
	tx.QueryRow("SELECT quantity FROM user_items WHERE player_id = $1 AND item_code = '00001'", playerID).Scan(&newGold)
	tx.QueryRow("SELECT quantity FROM user_items WHERE player_id = $1 AND item_code = '00000'", playerID).Scan(&newDiamond)

	err = tx.Commit()
	if err != nil {
		return 0, 0, err
	}

	return newGold, newDiamond, nil
}

type HeroTemplateDB struct {
	Code        string `db:"code" json:"code"`
	Name        string `db:"name" json:"name"`
	Rarity      string `db:"rarity" json:"rarity"`
	Element     string `db:"element" json:"element"`
	Role        string `db:"role" json:"role"`
	BaseHP      int32  `db:"base_hp" json:"base_hp"`
	BaseATK     int32  `db:"base_atk" json:"base_atk"`
	BaseDEF     int32  `db:"base_def" json:"base_def"`
	BaseSpeed   int32  `db:"base_speed" json:"base_speed"`
	GachaWeight int32  `db:"gacha_weight" json:"gacha_weight"`
	IsActive    bool   `db:"is_active" json:"is_active"`
}

func (r *adminRepo) GetAllHeroTemplates() ([]HeroTemplateDB, error) {
	db, err := r.getGameDB(1)
	if err != nil {
		return nil, err
	}

	var heroes []HeroTemplateDB
	err = db.Select(&heroes, "SELECT code, name, rarity, element, role, base_hp, base_atk, base_def, base_speed, gacha_weight, is_active FROM hero_templates ORDER BY code ASC")
	if err != nil {
		return nil, err
	}
	return heroes, nil
}

func (r *adminRepo) SyncHeroTemplates(req []HeroTemplateDB) error {
	db, err := r.getGameDB(1)
	if err != nil {
		return err
	}

	tx, err := db.Beginx()
	if err != nil {
		return err
	}
	defer tx.Rollback()

	// Insert or update all hero templates in req
	for _, h := range req {
		query := `
			INSERT INTO hero_templates (code, name, rarity, element, role, base_hp, base_atk, base_def, base_speed, gacha_weight, is_active)
			VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11)
			ON CONFLICT (code) DO UPDATE SET
				name = EXCLUDED.name,
				rarity = EXCLUDED.rarity,
				element = EXCLUDED.element,
				role = EXCLUDED.role,
				base_hp = EXCLUDED.base_hp,
				base_atk = EXCLUDED.base_atk,
				base_def = EXCLUDED.base_def,
				base_speed = EXCLUDED.base_speed,
				gacha_weight = EXCLUDED.gacha_weight,
				is_active = EXCLUDED.is_active
		`
		_, err = tx.Exec(query, h.Code, h.Name, h.Rarity, h.Element, h.Role, h.BaseHP, h.BaseATK, h.BaseDEF, h.BaseSpeed, h.GachaWeight, h.IsActive)
		if err != nil {
			return fmt.Errorf("failed to sync hero template %s: %v", h.Code, err)
		}
	}

	// Delete templates not present in req AND not referenced by any player_heroes
	if len(req) > 0 {
		var keepCodes []string
		for _, h := range req {
			keepCodes = append(keepCodes, h.Code)
		}
		query, args, err := sqlx.In(`
			DELETE FROM hero_templates 
			WHERE code NOT IN (?) 
			  AND code NOT IN (SELECT DISTINCT hero_code FROM player_heroes)
		`, keepCodes)
		if err == nil {
			query = tx.Rebind(query)
			_, err = tx.Exec(query, args...)
			if err != nil {
				return fmt.Errorf("failed to delete obsolete unreferenced hero templates: %v", err)
			}
		}
	} else {
		_, err = tx.Exec(`
			DELETE FROM hero_templates 
			WHERE code NOT IN (SELECT DISTINCT hero_code FROM player_heroes)
		`)
		if err != nil {
			return fmt.Errorf("failed to delete unreferenced hero templates: %v", err)
		}
	}

	return tx.Commit()
}

func (r *adminRepo) GMAddHero(zoneID int, userID int64, heroCode string) error {
	db, err := r.getGameDB(zoneID)
	if err != nil {
		return err
	}
	var playerID int64
	serverID := fmt.Sprintf("zone%d", zoneID)
	err = db.Get(&playerID, "SELECT id FROM players WHERE user_id = $1 AND server_id = $2", userID, serverID)
	if err != nil {
		return fmt.Errorf("không tìm thấy nhân vật ở zone %d: %v", zoneID, err)
	}

	_, err = db.Exec("INSERT INTO player_heroes (player_id, hero_code, traits) VALUES ($1, $2, '[]'::jsonb)", playerID, heroCode)
	return err
}

func (r *adminRepo) GMAddHeroWithTraits(zoneID int, userID int64, heroCode string, traits []string) error {
	db, err := r.getGameDB(zoneID)
	if err != nil {
		return err
	}
	var playerID int64
	serverID := fmt.Sprintf("zone%d", zoneID)
	err = db.Get(&playerID, "SELECT id FROM players WHERE user_id = $1 AND server_id = $2", userID, serverID)
	if err != nil {
		return fmt.Errorf("không tìm thấy nhân vật ở zone %d: %v", zoneID, err)
	}

	traitsJSON := "[]"
	if len(traits) > 0 {
		importJSON, err := json.Marshal(traits)
		if err == nil {
			traitsJSON = string(importJSON)
		}
	}

	_, err = db.Exec("INSERT INTO player_heroes (player_id, hero_code, traits) VALUES ($1, $2, $3::jsonb)", playerID, heroCode, traitsJSON)
	return err
}

func (r *adminRepo) CreateGiftCode(code string, rewardGold int64, rewardDiamond int64, rewardItems string, maxUses int) error {
	db, err := r.getGameDB(1) // Default zone DB holds the gift codes
	if err != nil {
		return err
	}

	if rewardItems == "" {
		rewardItems = "[]"
	}

	q := `
		INSERT INTO gift_codes (code, reward_gold, reward_diamond, reward_items, max_uses, used_count)
		VALUES ($1, $2, $3, $4::jsonb, $5, 0)
		ON CONFLICT (code) DO UPDATE 
		SET reward_gold = EXCLUDED.reward_gold,
		    reward_diamond = EXCLUDED.reward_diamond,
		    reward_items = EXCLUDED.reward_items,
		    max_uses = EXCLUDED.max_uses
	`
	_, err = db.Exec(q, code, rewardGold, rewardDiamond, rewardItems, maxUses)
	return err
}

func (r *adminRepo) GetAllShopItems() ([]ShopItemData, error) {
	db, err := r.getGameDB(1)
	if err != nil {
		return nil, err
	}
	var list []ShopItemData
	err = db.Select(&list, "SELECT id, shop_item_id, shop_type, item_code, amount, original_price::text as original_price, is_discountable FROM shop_items ORDER BY id ASC")
	if err != nil {
		return nil, err
	}
	return list, nil
}

func (r *adminRepo) SaveShopItem(c ShopItemData) error {
	db, err := r.getGameDB(1)
	if err != nil {
		return err
	}
	if c.OriginalPrice == "" {
		c.OriginalPrice = "[]"
	}
	var count int
	err = db.Get(&count, "SELECT COUNT(*) FROM shop_items WHERE shop_item_id = $1", c.ShopItemID)
	if err != nil {
		return err
	}
	if count > 0 {
		_, err = db.Exec(`
			UPDATE shop_items 
			SET shop_type = $2, item_code = $3, amount = $4, original_price = $5::jsonb, is_discountable = $6 
			WHERE shop_item_id = $1`,
			c.ShopItemID, c.ShopType, c.ItemCode, c.Amount, c.OriginalPrice, c.IsDiscountable)
	} else {
		_, err = db.Exec(`
			INSERT INTO shop_items (shop_item_id, shop_type, item_code, amount, original_price, is_discountable) 
			VALUES ($1, $2, $3, $4, $5::jsonb, $6)`,
			c.ShopItemID, c.ShopType, c.ItemCode, c.Amount, c.OriginalPrice, c.IsDiscountable)
	}
	return err
}

func (r *adminRepo) DeleteShopItem(shopItemID string) error {
	db, err := r.getGameDB(1)
	if err != nil {
		return err
	}
	_, err = db.Exec("DELETE FROM shop_items WHERE shop_item_id = $1", shopItemID)
	return err
}

func (r *adminRepo) CleanupOrphanedPlayers(zoneID int) (int64, error) {
	// 1. Get all user IDs from global database
	var userIDs []int64
	err := r.db.Select(&userIDs, "SELECT id FROM users")
	if err != nil {
		return 0, fmt.Errorf("failed to get users: %v", err)
	}

	// 2. Get the game database connection
	db, err := r.getGameDB(zoneID)
	if err != nil {
		return 0, fmt.Errorf("failed to get game db: %v", err)
	}

	// 3. Delete players whose user_id is not in the list of user IDs
	var query string
	var args []interface{}
	if len(userIDs) == 0 {
		query = "DELETE FROM players"
	} else {
		query, args, err = sqlx.In("DELETE FROM players WHERE user_id NOT IN (?)", userIDs)
		if err != nil {
			return 0, err
		}
		query = db.Rebind(query)
	}

	res, err := db.Exec(query, args...)
	if err != nil {
		return 0, fmt.Errorf("failed to delete players: %v", err)
	}

	rowsAffected, _ := res.RowsAffected()

	// Also clean up player_shops and player_shop_resets since they don't have CASCADE
	_, _ = db.Exec("DELETE FROM player_shops WHERE player_id NOT IN (SELECT id FROM players)")
	_, _ = db.Exec("DELETE FROM player_shop_resets WHERE player_id NOT IN (SELECT id FROM players)")

	return rowsAffected, nil
}




