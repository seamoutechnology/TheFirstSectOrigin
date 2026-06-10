package repository

import (
	"context"
	"errors"
	"fmt"
	"time"

	"strings"

	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"
)

var ErrNotFound = errors.New("record not found")

type Player struct {
	ID           int64
	UserID       int64
	ServerID     string
	Nickname     string
	Level        int32
	Exp          int64
	Gold         int64
	Diamond      int64
	Stamina      int32
	MaxStamina   int32
	LastStaminaAt time.Time
	CreatedAt    time.Time
	UpdatedAt    time.Time
}

type PlayerBuilding struct {
	ID            int64
	PlayerID      int64
	BuildingCode  string
	BuildingName  string
	MaxLevel      int32
	Level         int32
	UpgradeEndAt  *time.Time
	LastCollectAt time.Time
}

type PlayerHero struct {
	ID        int64
	PlayerID  int64
	HeroCode  string
	Name      string
	Rarity    string
	Element   string
	Role      string
	Level     int32
	Star      int32
	Exp       int64
	BaseHP    int32
	BaseATK   int32
	BaseDEF   int32
	BaseSpeed int32
}

type HeroTemplate struct {
	Code        string
	Name        string
	Rarity      string
	Element     string
	Role        string
	BaseHP      int32
	BaseATK     int32
	BaseDEF     int32
	BaseSpeed   int32
	GachaWeight int32
}

type GachaBanner struct {
	ID          int32
	Name        string
	Description string
	CostDiamond int32
	PityCount   int32
	IsActive    bool
	EndAt       *time.Time
}

type VersionConfig struct {
	Platform           string
	ClientVersion      string
	AddressableVersion string
	CatalogURL         string
	ForceUpdate        bool
	UpdateDesc         string
}

type PlayerRepository struct {
	db *pgxpool.Pool
}

func NewPlayerRepository(db *pgxpool.Pool) *PlayerRepository {
	return &PlayerRepository{db: db}
}

func (r *PlayerRepository) Create(ctx context.Context, userID int64, serverID string, nickname string) (*Player, error) {
	q := `
		INSERT INTO players (user_id, server_id, nickname)
		VALUES ($1, $2, $3)
		RETURNING id, user_id, server_id, nickname, level, exp, gold, diamond, stamina, max_stamina, last_stamina_at, created_at, updated_at
	`
	p := &Player{}
	err := r.db.QueryRow(ctx, q, userID, serverID, nickname).Scan(
		&p.ID, &p.UserID, &p.ServerID, &p.Nickname, &p.Level, &p.Exp, &p.Gold, &p.Diamond,
		&p.Stamina, &p.MaxStamina, &p.LastStaminaAt, &p.CreatedAt, &p.UpdatedAt,
	)
	return p, err
}

func (r *PlayerRepository) FindByNickname(ctx context.Context, nickname string, serverID string) (*Player, error) {
	q := `
		SELECT id, user_id, server_id, nickname, level, exp, gold, diamond, stamina, max_stamina, last_stamina_at, created_at, updated_at
		FROM players WHERE LOWER(nickname) = LOWER($1) AND server_id = $2
	`
	p := &Player{}
	err := r.db.QueryRow(ctx, q, nickname, serverID).Scan(
		&p.ID, &p.UserID, &p.ServerID, &p.Nickname, &p.Level, &p.Exp, &p.Gold, &p.Diamond,
		&p.Stamina, &p.MaxStamina, &p.LastStaminaAt, &p.CreatedAt, &p.UpdatedAt,
	)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, ErrNotFound
	}
	return p, err
}

func (r *PlayerRepository) FindByUserID(ctx context.Context, userID int64, serverID string) (*Player, error) {
	q := `
		SELECT id, user_id, server_id, nickname, level, exp, gold, diamond, stamina, max_stamina, last_stamina_at, created_at, updated_at
		FROM players WHERE user_id = $1 AND server_id = $2
	`
	p := &Player{}
	err := r.db.QueryRow(ctx, q, userID, serverID).Scan(
		&p.ID, &p.UserID, &p.ServerID, &p.Nickname, &p.Level, &p.Exp, &p.Gold, &p.Diamond,
		&p.Stamina, &p.MaxStamina, &p.LastStaminaAt, &p.CreatedAt, &p.UpdatedAt,
	)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, ErrNotFound
	}
	return p, err
}

func (r *PlayerRepository) UpdateResources(ctx context.Context, playerID int64, goldDelta, diamondDelta int64) (*Player, error) {
	q := `
		UPDATE players
		SET gold    = gold + $2,
		    diamond = diamond + $3,
		    updated_at = NOW()
		WHERE id = $1
		RETURNING id, user_id, nickname, level, exp, gold, diamond, stamina, max_stamina, last_stamina_at, created_at, updated_at
	`
	p := &Player{}
	err := r.db.QueryRow(ctx, q, playerID, goldDelta, diamondDelta).Scan(
		&p.ID, &p.UserID, &p.Nickname, &p.Level, &p.Exp, &p.Gold, &p.Diamond,
		&p.Stamina, &p.MaxStamina, &p.LastStaminaAt, &p.CreatedAt, &p.UpdatedAt,
	)
	return p, err
}

func (r *PlayerRepository) InitPlayerBuildings(ctx context.Context, playerID int64) error {
	q := `
		INSERT INTO player_buildings (player_id, building_code)
		SELECT $1, code FROM buildings
		ON CONFLICT DO NOTHING
	`
	_, err := r.db.Exec(ctx, q, playerID)
	return err
}

func (r *PlayerRepository) GetPlayerBuildings(ctx context.Context, playerID int64) ([]*PlayerBuilding, error) {
	q := `
		SELECT pb.id, pb.player_id, pb.building_code, b.name, b.max_level,
		       pb.level, pb.upgrade_end_at, pb.last_collect_at
		FROM player_buildings pb
		JOIN buildings b ON b.code = pb.building_code
		WHERE pb.player_id = $1
		ORDER BY pb.building_code
	`
	rows, err := r.db.Query(ctx, q, playerID)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var result []*PlayerBuilding
	for rows.Next() {
		b := &PlayerBuilding{}
		if err := rows.Scan(&b.ID, &b.PlayerID, &b.BuildingCode, &b.BuildingName,
			&b.MaxLevel, &b.Level, &b.UpgradeEndAt, &b.LastCollectAt); err != nil {
			return nil, err
		}
		
		// Lazy upgrade completion check: nếu thời gian nâng cấp đã hết → tăng level và xóa timer
		if b.UpgradeEndAt != nil && time.Now().After(*b.UpgradeEndAt) {
			_, updateErr := r.db.Exec(ctx,
				"UPDATE player_buildings SET level = level + 1, upgrade_end_at = NULL WHERE id = $1 AND upgrade_end_at IS NOT NULL",
				b.ID,
			)
			if updateErr == nil {
				b.Level++
				b.UpgradeEndAt = nil
			}
		}
		
		result = append(result, b)
	}
	return result, nil
}

func (r *PlayerRepository) UpgradeBuilding(ctx context.Context, playerID int64, instanceID int64, endAt time.Time) (*PlayerBuilding, error) {
	// FIX: Chỉ set upgrade_end_at, KHÔNG tăng level ngay.
	// Level sẽ được tăng sau khi upgrade_end_at hết hạn
	// (xử lý lazy trong GetPlayerBuildings).
	q := `
		UPDATE player_buildings SET upgrade_end_at = $3
		WHERE player_id = $1 AND id = $2 AND upgrade_end_at IS NULL
		RETURNING id, player_id, building_code, level, upgrade_end_at, last_collect_at
	`
	b := &PlayerBuilding{}
	err := r.db.QueryRow(ctx, q, playerID, instanceID, endAt).Scan(
		&b.ID, &b.PlayerID, &b.BuildingCode, &b.Level, &b.UpgradeEndAt, &b.LastCollectAt,
	)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, ErrNotFound
	}
	return b, err
}

func (r *PlayerRepository) SpeedUpBuilding(ctx context.Context, playerID int64, instanceID int64, diamondCost int64) (*PlayerBuilding, *Player, error) {
	// 1. Deduct user diamonds
	var p Player
	deductQuery := `
		UPDATE players SET diamond = diamond - $2
		WHERE id = $1 AND diamond >= $2
		RETURNING id, user_id, server_id, nickname, level, exp, gold, diamond, stamina, max_stamina, last_stamina_at
	`
	err := r.db.QueryRow(ctx, deductQuery, playerID, diamondCost).Scan(
		&p.ID, &p.UserID, &p.ServerID, &p.Nickname, &p.Level, &p.Exp, &p.Gold, &p.Diamond, &p.Stamina, &p.MaxStamina, &p.LastStaminaAt,
	)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, nil, errors.New("không đủ kim cương hoặc người chơi không tồn tại")
		}
		return nil, nil, err
	}

	// 2. Clear upgrade_end_at and increase level
	q := `
		UPDATE player_buildings SET level = level + 1, upgrade_end_at = NULL
		WHERE player_id = $1 AND id = $2 AND upgrade_end_at IS NOT NULL
		RETURNING id, player_id, building_code, level, upgrade_end_at, last_collect_at
	`
	b := &PlayerBuilding{}
	err = r.db.QueryRow(ctx, q, playerID, instanceID).Scan(
		&b.ID, &b.PlayerID, &b.BuildingCode, &b.Level, &b.UpgradeEndAt, &b.LastCollectAt,
	)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, nil, ErrNotFound
	}
	return b, &p, err
}

func (r *PlayerRepository) CollectGold(ctx context.Context, playerID int64, instanceID int64) (int64, error) {
	q := `
		UPDATE player_buildings
		SET last_collect_at = NOW()
		WHERE player_id = $1 AND id = $2
		RETURNING level, last_collect_at,
		          EXTRACT(EPOCH FROM (NOW() - last_collect_at)) / 3600 AS hours
	`
	var level int32
	var lastCollect time.Time
	var hours float64
	err := r.db.QueryRow(ctx, q, playerID, instanceID).Scan(&level, &lastCollect, &hours)
	if err != nil {
		return 0, err
	}
	if hours > 12 {
		hours = 12
	}
	gold := int64(float64(level) * 10 * hours)
	return gold, nil
}

func (r *PlayerRepository) AddPlayerBuilding(ctx context.Context, playerID int64, buildingCode string) (*PlayerBuilding, error) {
	q := `
		INSERT INTO player_buildings (player_id, building_code, level, last_collect_at)
		VALUES ($1, $2, 1, NOW())
		RETURNING id, player_id, building_code, level, upgrade_end_at, last_collect_at
	`
	b := &PlayerBuilding{}
	err := r.db.QueryRow(ctx, q, playerID, buildingCode).Scan(
		&b.ID, &b.PlayerID, &b.BuildingCode, &b.Level, &b.UpgradeEndAt, &b.LastCollectAt,
	)
	return b, err
}

func (r *PlayerRepository) DeletePlayerBuilding(ctx context.Context, playerID int64, instanceID int64) error {
	q := `DELETE FROM player_buildings WHERE player_id = $1 AND id = $2`
	_, err := r.db.Exec(ctx, q, playerID, instanceID)
	return err
}

func (r *PlayerRepository) GetPlayerHeroes(ctx context.Context, playerID int64) ([]*PlayerHero, error) {
	q := `
		SELECT ph.id, ph.player_id, ph.hero_code, ht.name, ht.rarity, ht.element, ht.role,
		       ph.level, ph.star, ph.exp, ht.base_hp, ht.base_atk, ht.base_def, ht.base_speed
		FROM player_heroes ph
		JOIN hero_templates ht ON ht.code = ph.hero_code
		WHERE ph.player_id = $1
		ORDER BY ht.rarity DESC, ph.level DESC
	`
	rows, err := r.db.Query(ctx, q, playerID)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var result []*PlayerHero
	for rows.Next() {
		h := &PlayerHero{}
		if err := rows.Scan(&h.ID, &h.PlayerID, &h.HeroCode, &h.Name, &h.Rarity, &h.Element, &h.Role,
			&h.Level, &h.Star, &h.Exp, &h.BaseHP, &h.BaseATK, &h.BaseDEF, &h.BaseSpeed); err != nil {
			return nil, err
		}
		result = append(result, h)
	}
	return result, nil
}

func (r *PlayerRepository) AddHero(ctx context.Context, playerID int64, heroCode string) (*PlayerHero, error) {
	q := `
		INSERT INTO player_heroes (player_id, hero_code) VALUES ($1, $2)
		RETURNING id, player_id, hero_code, level, star, exp
	`
	h := &PlayerHero{PlayerID: playerID, HeroCode: heroCode}
	err := r.db.QueryRow(ctx, q, playerID, heroCode).Scan(&h.ID, &h.PlayerID, &h.HeroCode, &h.Level, &h.Star, &h.Exp)
	return h, err
}

func (r *PlayerRepository) GetActiveBanners(ctx context.Context) ([]*GachaBanner, error) {
	q := `SELECT id, name, description, cost_diamond, pity_count, is_active, end_at FROM gacha_banners WHERE is_active = TRUE`
	rows, err := r.db.Query(ctx, q)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var result []*GachaBanner
	for rows.Next() {
		b := &GachaBanner{}
		if err := rows.Scan(&b.ID, &b.Name, &b.Description, &b.CostDiamond, &b.PityCount, &b.IsActive, &b.EndAt); err != nil {
			return nil, err
		}
		result = append(result, b)
	}
	return result, nil
}

func (r *PlayerRepository) GetGachaHeroPool(ctx context.Context) ([]*HeroTemplate, error) {
	q := `SELECT code, name, rarity, element, role, base_hp, base_atk, base_def, base_speed, gacha_weight FROM hero_templates WHERE is_active = TRUE`
	rows, err := r.db.Query(ctx, q)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var result []*HeroTemplate
	for rows.Next() {
		h := &HeroTemplate{}
		if err := rows.Scan(&h.Code, &h.Name, &h.Rarity, &h.Element, &h.Role,
			&h.BaseHP, &h.BaseATK, &h.BaseDEF, &h.BaseSpeed, &h.GachaWeight); err != nil {
			return nil, err
		}
		result = append(result, h)
	}
	return result, nil
}

func (r *PlayerRepository) GetOrCreatePity(ctx context.Context, playerID int64, bannerID int32) (int32, error) {
	q := `
		INSERT INTO player_gacha_pity (player_id, banner_id, pull_count)
		VALUES ($1, $2, 0)
		ON CONFLICT (player_id, banner_id) DO UPDATE SET pull_count = player_gacha_pity.pull_count
		RETURNING pull_count
	`
	var count int32
	err := r.db.QueryRow(ctx, q, playerID, bannerID).Scan(&count)
	return count, err
}

func (r *PlayerRepository) UpdatePity(ctx context.Context, playerID int64, bannerID int32, newCount int32) error {
	q := `UPDATE player_gacha_pity SET pull_count = $3 WHERE player_id = $1 AND banner_id = $2`
	_, err := r.db.Exec(ctx, q, playerID, bannerID, newCount)
	return err
}

func (r *PlayerRepository) SetFormation(ctx context.Context, playerID int64, slots map[int32]int64) error {
	tx, err := r.db.Begin(ctx)
	if err != nil {
		return err
	}
	defer tx.Rollback(ctx)

	if _, err := tx.Exec(ctx, `DELETE FROM player_formations WHERE player_id = $1`, playerID); err != nil {
		return err
	}

	for pos, heroID := range slots {
		if _, err := tx.Exec(ctx, `INSERT INTO player_formations (player_id, position, player_hero_id) VALUES ($1, $2, $3)`, playerID, pos, heroID); err != nil {
			return err
		}
	}
	return tx.Commit(ctx)
}

func (r *PlayerRepository) GetVersionConfig(ctx context.Context, platform string) (*VersionConfig, error) {
	q := `
		SELECT platform, client_version, addressable_version, catalog_url, force_update, update_desc
		FROM version_configs WHERE platform = $1
	`
	v := &VersionConfig{}
	err := r.db.QueryRow(ctx, q, platform).Scan(
		&v.Platform, &v.ClientVersion, &v.AddressableVersion, &v.CatalogURL, &v.ForceUpdate, &v.UpdateDesc,
	)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, ErrNotFound
	}
	return v, err
}


func (r *PlayerRepository) SaveCutscene(ctx context.Context, id string, jsonData string) error {
	query := `
		INSERT INTO cutscenes (id, json_data, updated_at) 
		VALUES ($1, $2, CURRENT_TIMESTAMP)
		ON CONFLICT (id) DO UPDATE SET json_data = EXCLUDED.json_data, updated_at = EXCLUDED.updated_at
	`
	_, err := r.db.Exec(ctx, query, id, jsonData)
	return err
}

func (r *PlayerRepository) GetCutscene(ctx context.Context, id string) (string, error) {
	var jsonData string
	err := r.db.QueryRow(ctx, "SELECT json_data FROM cutscenes WHERE id = $1", id).Scan(&jsonData)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return "", ErrNotFound
		}
		return "", err
	}
	return jsonData, nil
}

func (r *PlayerRepository) ListCutscenes(ctx context.Context) ([]string, error) {
	rows, err := r.db.Query(ctx, "SELECT id FROM cutscenes ORDER BY id")
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var ids []string
	for rows.Next() {
		var id string
		if err := rows.Scan(&id); err == nil {
			ids = append(ids, id)
		}
	}
	return ids, rows.Err()
}


func (r *PlayerRepository) SaveAdminMap(ctx context.Context, id string, jsonData string) error {
	query := `
		INSERT INTO admin_maps (id, json_data, updated_at) 
		VALUES ($1, $2, CURRENT_TIMESTAMP)
		ON CONFLICT (id) DO UPDATE SET json_data = EXCLUDED.json_data, updated_at = EXCLUDED.updated_at
	`
	_, err := r.db.Exec(ctx, query, id, jsonData)
	return err
}

func (r *PlayerRepository) GetAdminMap(ctx context.Context, id string) (string, error) {
	var jsonData string
	err := r.db.QueryRow(ctx, "SELECT json_data FROM admin_maps WHERE id = $1", id).Scan(&jsonData)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return "", ErrNotFound
		}
		return "", err
	}
	return jsonData, nil
}

func (r *PlayerRepository) SavePlayerMap(ctx context.Context, playerID int64, jsonData string) error {
	query := `
		INSERT INTO player_maps (player_id, json_data, updated_at) 
		VALUES ($1, $2, CURRENT_TIMESTAMP)
		ON CONFLICT (player_id) DO UPDATE SET json_data = EXCLUDED.json_data, updated_at = EXCLUDED.updated_at
	`
	_, err := r.db.Exec(ctx, query, playerID, jsonData)
	return err
}

func (r *PlayerRepository) GetPlayerMap(ctx context.Context, playerID int64) (string, error) {
	var jsonData string
	err := r.db.QueryRow(ctx, "SELECT json_data FROM player_maps WHERE player_id = $1", playerID).Scan(&jsonData)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return "", ErrNotFound
		}
		return "", err
	}
	return jsonData, nil
}

func (r *PlayerRepository) GetPlayerInventory(ctx context.Context, playerID int64) ([]*UserItem, error) {
	rows, err := r.db.Query(ctx, "SELECT id, player_id, item_code, quantity, stats FROM user_items WHERE player_id = $1", playerID)
	if err != nil {
		return nil, err
	}
	
	var result []*UserItem
	for rows.Next() {
		item := &UserItem{}
		var statsBytes []byte
		if err := rows.Scan(&item.ID, &item.PlayerID, &item.ItemCode, &item.Quantity, &statsBytes); err != nil {
			rows.Close()
			return nil, err
		}
		item.Stats = string(statsBytes)
		result = append(result, item)
	}
	rows.Close()

	// Developer Fallback: Tự động cấp nguyên liệu mẫu để test UI nếu kho đồ trống
	if len(result) == 0 {
		_, _ = r.db.Exec(ctx, `
			INSERT INTO user_items (player_id, item_code, quantity, stats)
			VALUES 
				($1, '00000', 999, '[]'),
				($1, '00002', 500, '[]'),
				($1, '00003', 500, '[]')
		`, playerID)
		
		// Query lại
		rows2, err2 := r.db.Query(ctx, "SELECT id, player_id, item_code, quantity, stats FROM user_items WHERE player_id = $1", playerID)
		if err2 == nil {
			defer rows2.Close()
			for rows2.Next() {
				item := &UserItem{}
				var statsBytes []byte
				if errScan := rows2.Scan(&item.ID, &item.PlayerID, &item.ItemCode, &item.Quantity, &statsBytes); errScan == nil {
					item.Stats = string(statsBytes)
					result = append(result, item)
				}
			}
		}
	}

	return result, nil
}

func (r *PlayerRepository) DeductUserItems(ctx context.Context, playerID int64, items map[string]int32) error {
	tx, err := r.db.Begin(ctx)
	if err != nil {
		return err
	}
	defer tx.Rollback(ctx)

	var missingDetails []string

	for itemCode, qty := range items {
		var currentQty int32
		err = tx.QueryRow(ctx, "SELECT quantity FROM user_items WHERE player_id = $1 AND item_code = $2 FOR UPDATE", playerID, itemCode).Scan(&currentQty)
		
		var nameKey string
		_ = tx.QueryRow(ctx, "SELECT name_key FROM item_configs WHERE item_code = $1", itemCode).Scan(&nameKey)
		displayName := nameKey
		if displayName == "" {
			displayName = itemCode
		}

		if err != nil {
			if errors.Is(err, pgx.ErrNoRows) {
				missingDetails = append(missingDetails, fmt.Sprintf("%s (Thiếu %d)", displayName, qty))
			} else {
				return err
			}
		} else if currentQty < qty {
			missingDetails = append(missingDetails, fmt.Sprintf("%s (Có %d/%d, Thiếu %d)", displayName, currentQty, qty, qty-currentQty))
		}
	}

	if len(missingDetails) > 0 {
		return fmt.Errorf("Thiếu nguyên liệu nâng cấp:\n- %s", strings.Join(missingDetails, "\n- "))
	}

	// Thực hiện trừ nếu đủ hết
	for itemCode, qty := range items {
		var currentQty int32
		_ = tx.QueryRow(ctx, "SELECT quantity FROM user_items WHERE player_id = $1 AND item_code = $2", playerID, itemCode).Scan(&currentQty)

		if currentQty == qty {
			_, err = tx.Exec(ctx, "DELETE FROM user_items WHERE player_id = $1 AND item_code = $2", playerID, itemCode)
		} else {
			_, err = tx.Exec(ctx, "UPDATE user_items SET quantity = quantity - $1 WHERE player_id = $2 AND item_code = $3", qty, playerID, itemCode)
		}
		if err != nil {
			return err
		}
	}

	return tx.Commit(ctx)
}

func (r *PlayerRepository) GetPlayerItemInstance(ctx context.Context, playerID int64, itemID int64) (*UserItem, error) {
	item := &UserItem{}
	var statsBytes []byte
	err := r.db.QueryRow(ctx, "SELECT id, player_id, item_code, quantity, stats FROM user_items WHERE id = $1 AND player_id = $2", itemID, playerID).Scan(
		&item.ID, &item.PlayerID, &item.ItemCode, &item.Quantity, &statsBytes)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, ErrNotFound
		}
		return nil, err
	}
	item.Stats = string(statsBytes)
	return item, nil
}

func (r *PlayerRepository) GetItemConfig(ctx context.Context, itemCode string) (*RepoItemConfig, error) {
	cfg := &RepoItemConfig{}
	var sourcesBytes, effectsBytes []byte
	err := r.db.QueryRow(ctx, "SELECT item_code, name_key, type, rarity, icon, desc_key, max_stack, sources, effects FROM item_configs WHERE item_code = $1", itemCode).Scan(
		&cfg.ItemCode, &cfg.NameKey, &cfg.Type, &cfg.Rarity, &cfg.Icon, &cfg.DescKey, &cfg.MaxStack, &sourcesBytes, &effectsBytes)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, ErrNotFound
		}
		return nil, err
	}
	cfg.Sources = string(sourcesBytes)
	cfg.Effects = string(effectsBytes)
	return cfg, nil
}

func (r *PlayerRepository) GetAllItemConfigs(ctx context.Context) ([]*RepoItemConfig, error) {
	rows, err := r.db.Query(ctx, "SELECT item_code, name_key, type, rarity, icon, desc_key, max_stack, sources, effects FROM item_configs")
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var result []*RepoItemConfig
	for rows.Next() {
		cfg := &RepoItemConfig{}
		var sourcesBytes, effectsBytes []byte
		err := rows.Scan(&cfg.ItemCode, &cfg.NameKey, &cfg.Type, &cfg.Rarity, &cfg.Icon, &cfg.DescKey, &cfg.MaxStack, &sourcesBytes, &effectsBytes)
		if err != nil {
			return nil, err
		}
		cfg.Sources = string(sourcesBytes)
		cfg.Effects = string(effectsBytes)
		result = append(result, cfg)
	}
	return result, rows.Err()
}

func (r *PlayerRepository) UseItemTransaction(ctx context.Context, playerID int64, itemID int64, quantity int32, useType string, codeParam string, valueParam int32) error {
	tx, err := r.db.Begin(ctx)
	if err != nil {
		return err
	}
	defer tx.Rollback(ctx)

	// 1. Lock and check quantity
	var currentQty int32
	var itemCode string
	err = tx.QueryRow(ctx, "SELECT item_code, quantity FROM user_items WHERE id = $1 AND player_id = $2 FOR UPDATE", itemID, playerID).Scan(&itemCode, &currentQty)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return errors.New("vật phẩm không tồn tại")
		}
		return err
	}

	if currentQty < quantity {
		return errors.New("không đủ số lượng vật phẩm")
	}

	// 2. Deduct inventory
	if currentQty == quantity {
		_, err = tx.Exec(ctx, "DELETE FROM user_items WHERE id = $1 AND player_id = $2", itemID, playerID)
	} else {
		_, err = tx.Exec(ctx, "UPDATE user_items SET quantity = quantity - $1 WHERE id = $2 AND player_id = $3", quantity, itemID, playerID)
	}
	if err != nil {
		return err
	}

	// 3. Apply changes based on useType
	switch useType {
	case "CURRENCY":
		// codeParam: "gold", "diamond"
		if codeParam == "gold" {
			_, err = tx.Exec(ctx, "UPDATE players SET gold = gold + $1 WHERE id = $2", int64(valueParam)*int64(quantity), playerID)
		} else if codeParam == "diamond" {
			_, err = tx.Exec(ctx, "UPDATE players SET diamond = diamond + $1 WHERE id = $2", int64(valueParam)*int64(quantity), playerID)
		}
	case "SKIN_UNLOCKER":
		// codeParam: skin_code
		_, err = tx.Exec(ctx, "INSERT INTO player_skins (player_id, skin_code, is_unlocked) VALUES ($1, $2, TRUE) ON CONFLICT (player_id, skin_code) DO NOTHING", playerID, codeParam)
	case "VIP_LICENSE":
		// valueParam: vipLevel, codeParam: days to add
		days := int32(30) // fallback default
		if valueParam <= 0 {
			valueParam = 1 // default VIP level 1
		}
		if codeParam != "" {
			var parsedDays int
			if _, errScan := fmt.Sscanf(codeParam, "%d", &parsedDays); errScan == nil {
				days = int32(parsedDays)
			}
		}
		
		var exists bool
		err = tx.QueryRow(ctx, "SELECT EXISTS(SELECT 1 FROM player_vip WHERE player_id = $1)", playerID).Scan(&exists)
		if err != nil {
			return err
		}

		duration := time.Duration(days*24) * time.Hour
		if exists {
			_, err = tx.Exec(ctx, `
				UPDATE player_vip 
				SET vip_level = $1, 
				    expire_at = CASE WHEN expire_at > NOW() THEN expire_at + $2::interval ELSE NOW() + $2::interval END,
				    updated_at = NOW() 
				WHERE player_id = $3`, valueParam, duration.String(), playerID)
		} else {
			_, err = tx.Exec(ctx, `
				INSERT INTO player_vip (player_id, vip_level, expire_at) 
				VALUES ($1, $2, NOW() + $3::interval)`, playerID, valueParam, duration.String())
		}
	case "FUNCTION_UNLOCKER":
		// codeParam: function_code
		_, err = tx.Exec(ctx, "INSERT INTO player_unlocked_functions (player_id, function_code) VALUES ($1, $2) ON CONFLICT (player_id, function_code) DO NOTHING", playerID, codeParam)
	case "CONSUMABLE":
		// codeParam: "stamina", "level", "exp" etc.
		if codeParam == "stamina" {
			_, err = tx.Exec(ctx, "UPDATE players SET stamina = LEAST(max_stamina, stamina + $1) WHERE id = $2", valueParam*quantity, playerID)
		} else if codeParam == "exp" {
			_, err = tx.Exec(ctx, "UPDATE players SET exp = exp + $1 WHERE id = $2", int64(valueParam)*int64(quantity), playerID)
		} else if codeParam == "level" {
			_, err = tx.Exec(ctx, "UPDATE players SET level = level + $1 WHERE id = $2", valueParam*quantity, playerID)
		}
	}

	if err != nil {
		return err
	}

	return tx.Commit(ctx)
}

func (r *PlayerRepository) GetMissionTemplates(ctx context.Context) ([]*DBMissionTemplate, error) {
	rows, err := r.db.Query(ctx, "SELECT mission_id, title, description, type, target_type, target_param, target_progress, rewards, created_at, updated_at FROM mission_templates")
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var result []*DBMissionTemplate
	for rows.Next() {
		m := &DBMissionTemplate{}
		var rewardsBytes []byte
		err := rows.Scan(&m.MissionID, &m.Title, &m.Description, &m.Type, &m.TargetType, &m.TargetParam, &m.TargetProgress, &rewardsBytes, &m.CreatedAt, &m.UpdatedAt)
		if err != nil {
			return nil, err
		}
		m.Rewards = string(rewardsBytes)
		result = append(result, m)
	}
	return result, rows.Err()
}

func (r *PlayerRepository) GetPlayerMissions(ctx context.Context, playerID int64) ([]*DBPlayerMission, error) {
	rows, err := r.db.Query(ctx, "SELECT player_id, mission_id, status, current_progress, updated_at FROM player_missions WHERE player_id = $1", playerID)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var result []*DBPlayerMission
	for rows.Next() {
		m := &DBPlayerMission{}
		err := rows.Scan(&m.PlayerID, &m.MissionID, &m.Status, &m.CurrentProgress, &m.UpdatedAt)
		if err != nil {
			return nil, err
		}
		result = append(result, m)
	}
	return result, rows.Err()
}

func (r *PlayerRepository) UpdatePlayerMissionProgress(ctx context.Context, playerID int64, missionID int32, progress int32, status int32) error {
	_, err := r.db.Exec(ctx, `
		UPDATE player_missions 
		SET current_progress = $1, status = $2, updated_at = NOW() 
		WHERE player_id = $3 AND mission_id = $4`, progress, status, playerID, missionID)
	return err
}

func (r *PlayerRepository) CreatePlayerMission(ctx context.Context, playerID int64, missionID int32, status int32) error {
	_, err := r.db.Exec(ctx, `
		INSERT INTO player_missions (player_id, mission_id, status, current_progress) 
		VALUES ($1, $2, $3, 0)
		ON CONFLICT (player_id, mission_id) DO NOTHING`, playerID, missionID, status)
	return err
}

func (r *PlayerRepository) ClaimMissionRewardDB(ctx context.Context, playerID int64, missionID int32, rewards map[string]int32) error {
	tx, err := r.db.Begin(ctx)
	if err != nil {
		return err
	}
	defer tx.Rollback(ctx)

	// Update mission status to REWARDED (4)
	var status int32
	err = tx.QueryRow(ctx, "SELECT status FROM player_missions WHERE player_id = $1 AND mission_id = $2 FOR UPDATE", playerID, missionID).Scan(&status)
	if err != nil {
		return err
	}
	if status != 3 { // pb.MissionStatus_COMPLETED
		return errors.New("nhiệm vụ chưa hoàn thành hoặc đã nhận thưởng")
	}

	_, err = tx.Exec(ctx, "UPDATE player_missions SET status = 4, updated_at = NOW() WHERE player_id = $1 AND mission_id = $2", playerID, missionID)
	if err != nil {
		return err
	}

	// Add rewards to player profile/inventory
	for itemCode, qty := range rewards {
		if itemCode == "gold" {
			_, err = tx.Exec(ctx, "UPDATE players SET gold = gold + $1 WHERE id = $2", int64(qty), playerID)
		} else if itemCode == "exp" {
			_, err = tx.Exec(ctx, "UPDATE players SET exp = exp + $1 WHERE id = $2", int64(qty), playerID)
		} else if itemCode == "diamond" {
			_, err = tx.Exec(ctx, "UPDATE players SET diamond = diamond + $1 WHERE id = $2", int64(qty), playerID)
		} else {
			// standard inventory item insert
			var exists bool
			err = tx.QueryRow(ctx, "SELECT EXISTS(SELECT 1 FROM user_items WHERE player_id = $1 AND item_code = $2)", playerID, itemCode).Scan(&exists)
			if err != nil {
				return err
			}
			if exists {
				_, err = tx.Exec(ctx, "UPDATE user_items SET quantity = quantity + $1 WHERE player_id = $2 AND item_code = $3", qty, playerID, itemCode)
			} else {
				_, err = tx.Exec(ctx, "INSERT INTO user_items (player_id, item_code, quantity, stats) VALUES ($1, $2, $3, '[]')", playerID, itemCode, qty)
			}
		}
		if err != nil {
			return err
		}
	}

	return tx.Commit(ctx)
}


