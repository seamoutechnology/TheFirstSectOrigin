package admin

import (
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
	ID         int    `db:"id"`
	Name       string `db:"name"`
	GatewayURL string `db:"gateway_url"`
	Status     string `db:"status"`
	IsActive   bool   `db:"is_active"`
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


func (r *adminRepo) GetUserInfo(userID int64) (UserInfo, error) {
	var info UserInfo
	info.UserID = userID
	_ = r.db.Get(&info.Email, "SELECT email FROM users WHERE id = $1", userID)
	
	db, _ := r.getGameDB(1)
	_ = db.QueryRow("SELECT nickname, level, bind_coin FROM players WHERE user_id = $1", userID).Scan(&info.SectName, &info.Level, &info.Money)
	
	return info, nil
}

func (r *adminRepo) GetUsersPaginated(limit, offset int, search string) (UserListResponse, error) {
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
	
	query, args, err := sqlx.In("SELECT user_id, nickname, level FROM players WHERE user_id IN (?)", userIDs)
	if err != nil {
		return resp, err
	}
	db, _ := r.getGameDB(1)
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
	var items []UserItem
	err = db.Select(&items, "SELECT id, user_id, item_code, quantity FROM user_items WHERE user_id = $1 ORDER BY id DESC", userID)
	return items, err
}

func (r *adminRepo) AddUserItem(zoneID int, userID int64, itemCode string, quantity int) error {
	db, err := r.getGameDB(zoneID)
	if err != nil {
		return err
	}
	var id int64
	err = db.QueryRow("SELECT id FROM user_items WHERE user_id = $1 AND item_code = $2 LIMIT 1", userID, itemCode).Scan(&id)
	
	if err == nil {
		_, err = db.Exec("UPDATE user_items SET quantity = quantity + $1 WHERE id = $2", quantity, id)
		return err
	}
	
	_, err = db.Exec("INSERT INTO user_items (user_id, item_code, quantity) VALUES ($1, $2, $3)", userID, itemCode, quantity)
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
	query := "SELECT item_code, name_key, type, rarity, icon, desc_key, max_stack, sources, effects FROM item_configs ORDER BY item_code ASC"
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
		INSERT INTO item_configs (item_code, name_key, type, rarity, icon, desc_key, max_stack, sources, effects)
		VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9)
		ON CONFLICT (item_code) DO UPDATE SET
			name_key = EXCLUDED.name_key,
			type = EXCLUDED.type,
			rarity = EXCLUDED.rarity,
			icon = EXCLUDED.icon,
			desc_key = EXCLUDED.desc_key,
			max_stack = EXCLUDED.max_stack,
			sources = EXCLUDED.sources,
			effects = EXCLUDED.effects,
			updated_at = CURRENT_TIMESTAMP
	`
	db, _ := r.getGameDB(1)
	_, err := db.Exec(query, c.ItemCode, c.NameKey, c.Type, c.Rarity, c.Icon, c.DescKey, c.MaxStack, c.Sources, c.Effects)
	return err
}

func (r *adminRepo) DeleteItemConfig(itemCode string) error {
	db, _ := r.getGameDB(1)
	_, err := db.Exec("DELETE FROM item_configs WHERE item_code = $1", itemCode)
	return err
}


func (r *adminRepo) GetAllEffectConfigs() ([]EffectConfigData, error) {
	var list []EffectConfigData
	query := "SELECT effect_code, name_key, desc_key, effect_type, value_type, min_value, max_value FROM effect_configs ORDER BY effect_code ASC"
	db, _ := r.getGameDB(1)
	err := db.Select(&list, query)
	return list, err
}

func (r *adminRepo) SaveEffectConfig(c EffectConfigData) error {
	query := `
		INSERT INTO effect_configs (effect_code, name_key, desc_key, effect_type, value_type, min_value, max_value)
		VALUES ($1, $2, $3, $4, $5, $6, $7)
		ON CONFLICT (effect_code) DO UPDATE SET
			name_key = EXCLUDED.name_key,
			desc_key = EXCLUDED.desc_key,
			effect_type = EXCLUDED.effect_type,
			value_type = EXCLUDED.value_type,
			min_value = EXCLUDED.min_value,
			max_value = EXCLUDED.max_value,
			updated_at = CURRENT_TIMESTAMP
	`
	db, _ := r.getGameDB(1)
	_, err := db.Exec(query, c.EffectCode, c.NameKey, c.DescKey, c.EffectType, c.ValueType, c.MinValue, c.MaxValue)
	return err
}

func (r *adminRepo) DeleteEffectConfig(effectCode string) error {
	db, _ := r.getGameDB(1)
	_, err := db.Exec("DELETE FROM effect_configs WHERE effect_code = $1", effectCode)
	return err
}
