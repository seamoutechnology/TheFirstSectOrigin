package repository

import (
	"context"
	"encoding/json"
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
	Power        int64
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
	Traits    string // JSON bytes of string array
	Skills    string // JSON bytes of learned skills
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
	CostItem    string
	CostGold    int32
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

func (r *PlayerRepository) RecoverStamina(ctx context.Context, p *Player) {
	if p == nil || p.Stamina >= p.MaxStamina {
		return
	}
	now := time.Now()
	elapsed := now.Sub(p.LastStaminaAt)
	if elapsed < 5*time.Minute {
		return
	}

	recovered := int32(elapsed.Seconds() / 300)
	if recovered <= 0 {
		return
	}

	newStamina := p.Stamina + recovered
	var newLastStaminaAt time.Time
	if newStamina >= p.MaxStamina {
		newStamina = p.MaxStamina
		newLastStaminaAt = now
	} else {
		newLastStaminaAt = p.LastStaminaAt.Add(time.Duration(recovered) * 5 * time.Minute)
	}

	_, err := r.db.Exec(ctx, "UPDATE players SET stamina = $1, last_stamina_at = $2 WHERE id = $3", newStamina, newLastStaminaAt, p.ID)
	if err == nil {
		p.Stamina = newStamina
		p.LastStaminaAt = newLastStaminaAt
	}
}

func (r *PlayerRepository) Create(ctx context.Context, userID int64, serverID string, nickname string) (*Player, error) {
	q := `
		INSERT INTO players (user_id, server_id, nickname)
		VALUES ($1, $2, $3)
		RETURNING id, user_id, server_id, nickname, level, exp, gold, diamond, stamina, max_stamina, last_stamina_at, power, created_at, updated_at
	`
	p := &Player{}
	err := r.db.QueryRow(ctx, q, userID, serverID, nickname).Scan(
		&p.ID, &p.UserID, &p.ServerID, &p.Nickname, &p.Level, &p.Exp, &p.Gold, &p.Diamond,
		&p.Stamina, &p.MaxStamina, &p.LastStaminaAt, &p.Power, &p.CreatedAt, &p.UpdatedAt,
	)
	return p, err
}

func (r *PlayerRepository) FindByNickname(ctx context.Context, nickname string, serverID string) (*Player, error) {
	q := `
		SELECT id, user_id, server_id, nickname, level, exp, gold, diamond, stamina, max_stamina, last_stamina_at, power, created_at, updated_at
		FROM players WHERE LOWER(nickname) = LOWER($1) AND server_id = $2
	`
	p := &Player{}
	err := r.db.QueryRow(ctx, q, nickname, serverID).Scan(
		&p.ID, &p.UserID, &p.ServerID, &p.Nickname, &p.Level, &p.Exp, &p.Gold, &p.Diamond,
		&p.Stamina, &p.MaxStamina, &p.LastStaminaAt, &p.Power, &p.CreatedAt, &p.UpdatedAt,
	)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, ErrNotFound
	}
	if err == nil {
		r.RecoverStamina(ctx, p)
	}
	return p, err
}

func (r *PlayerRepository) FindByUserID(ctx context.Context, userID int64, serverID string) (*Player, error) {
	q := `
		SELECT id, user_id, server_id, nickname, level, exp, gold, diamond, stamina, max_stamina, last_stamina_at, power, created_at, updated_at
		FROM players WHERE user_id = $1 AND server_id = $2
	`
	p := &Player{}
	err := r.db.QueryRow(ctx, q, userID, serverID).Scan(
		&p.ID, &p.UserID, &p.ServerID, &p.Nickname, &p.Level, &p.Exp, &p.Gold, &p.Diamond,
		&p.Stamina, &p.MaxStamina, &p.LastStaminaAt, &p.Power, &p.CreatedAt, &p.UpdatedAt,
	)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, ErrNotFound
	}
	if err == nil {
		r.RecoverStamina(ctx, p)
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
		RETURNING id, user_id, nickname, level, exp, gold, diamond, stamina, max_stamina, last_stamina_at, power, created_at, updated_at
	`
	p := &Player{}
	err := r.db.QueryRow(ctx, q, playerID, goldDelta, diamondDelta).Scan(
		&p.ID, &p.UserID, &p.Nickname, &p.Level, &p.Exp, &p.Gold, &p.Diamond,
		&p.Stamina, &p.MaxStamina, &p.LastStaminaAt, &p.Power, &p.CreatedAt, &p.UpdatedAt,
	)
	if err == nil {
		r.RecoverStamina(ctx, p)
	}
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
		RETURNING id, user_id, server_id, nickname, level, exp, gold, diamond, stamina, max_stamina, last_stamina_at, power
	`
	err := r.db.QueryRow(ctx, deductQuery, playerID, diamondCost).Scan(
		&p.ID, &p.UserID, &p.ServerID, &p.Nickname, &p.Level, &p.Exp, &p.Gold, &p.Diamond, &p.Stamina, &p.MaxStamina, &p.LastStaminaAt, &p.Power,
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
		       ph.level, ph.star, ph.exp, ht.base_hp, ht.base_atk, ht.base_def, ht.base_speed, ph.traits, ph.skills
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
		var traitsBytes []byte
		var skillsBytes []byte
		if err := rows.Scan(&h.ID, &h.PlayerID, &h.HeroCode, &h.Name, &h.Rarity, &h.Element, &h.Role,
			&h.Level, &h.Star, &h.Exp, &h.BaseHP, &h.BaseATK, &h.BaseDEF, &h.BaseSpeed, &traitsBytes, &skillsBytes); err != nil {
			return nil, err
		}
		h.Traits = string(traitsBytes)
		h.Skills = string(skillsBytes)
		result = append(result, h)
	}

	if len(result) == 0 {
		// Tự động cấp 3 tướng tân thủ mặc định nếu tài khoản chưa có tướng nào
		defaultHeroes := []string{"FIRE_WARRIOR_01", "WATER_TANK_01", "WOOD_HEALER_01"}
		defaultSlots := make(map[int32]int64)
		for i, code := range defaultHeroes {
			hero, err := r.AddHero(ctx, playerID, code)
			if err == nil && hero != nil {
				defaultSlots[int32(i)] = hero.ID
			}
		}
		_ = r.SetFormation(ctx, playerID, defaultSlots)

		// Truy vấn lại để lấy thông tin các tướng vừa thêm
		rows2, err := r.db.Query(ctx, q, playerID)
		if err == nil {
			defer rows2.Close()
			for rows2.Next() {
				h := &PlayerHero{}
				var traitsBytes []byte
				var skillsBytes []byte
				if err := rows2.Scan(&h.ID, &h.PlayerID, &h.HeroCode, &h.Name, &h.Rarity, &h.Element, &h.Role,
					&h.Level, &h.Star, &h.Exp, &h.BaseHP, &h.BaseATK, &h.BaseDEF, &h.BaseSpeed, &traitsBytes, &skillsBytes); err == nil {
					h.Traits = string(traitsBytes)
					h.Skills = string(skillsBytes)
					result = append(result, h)
				}
			}
		}
	}

	return result, nil
}

func (r *PlayerRepository) AddHero(ctx context.Context, playerID int64, heroCode string) (*PlayerHero, error) {
	q := `
		INSERT INTO player_heroes (player_id, hero_code, traits, skills) 
		VALUES ($1, $2, '[]'::jsonb, 
			CASE 
				WHEN $2 = 'DARK_ASSASSIN_01' THEN '[{"skill_code": "skill_shadow_strike", "level": 1}]'::jsonb
				WHEN $2 IN ('FIRE_GENERAL_01', 'LIGHT_MAGE_01') THEN '[{"skill_code": "skill_fireball", "level": 1}]'::jsonb
				WHEN $2 = 'WATER_TANK_01' THEN '[{"skill_code": "skill_shield", "level": 1}]'::jsonb
				WHEN $2 = 'WOOD_HEALER_01' THEN '[{"skill_code": "skill_heal", "level": 1}]'::jsonb
				ELSE '[{"skill_code": "skill_slash", "level": 1}]'::jsonb
			END
		)
		RETURNING id, player_id, hero_code, level, star, exp, traits, skills
	`
	h := &PlayerHero{PlayerID: playerID, HeroCode: heroCode}
	var traitsBytes []byte
	var skillsBytes []byte
	err := r.db.QueryRow(ctx, q, playerID, heroCode).Scan(&h.ID, &h.PlayerID, &h.HeroCode, &h.Level, &h.Star, &h.Exp, &traitsBytes, &skillsBytes)
	h.Traits = string(traitsBytes)
	h.Skills = string(skillsBytes)
	if err == nil {
		_ = r.UpdatePlayerPower(ctx, playerID)
	}
	return h, err
}

func (r *PlayerRepository) GetActiveBanners(ctx context.Context) ([]*GachaBanner, error) {
	q := `SELECT id, name, description, cost_diamond, pity_count, is_active, end_at, cost_item, cost_gold FROM gacha_banners WHERE is_active = TRUE`
	rows, err := r.db.Query(ctx, q)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var result []*GachaBanner
	for rows.Next() {
		b := &GachaBanner{}
		if err := rows.Scan(&b.ID, &b.Name, &b.Description, &b.CostDiamond, &b.PityCount, &b.IsActive, &b.EndAt, &b.CostItem, &b.CostGold); err != nil {
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

func (r *PlayerRepository) GetFormation(ctx context.Context, playerID int64) (map[int32]int64, error) {
	rows, err := r.db.Query(ctx, `SELECT position, player_hero_id FROM player_formations WHERE player_id = $1`, playerID)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	slots := make(map[int32]int64)
	for rows.Next() {
		var pos int32
		var heroID int64
		if err := rows.Scan(&pos, &heroID); err != nil {
			return nil, err
		}
		slots[pos] = heroID
	}
	return slots, nil
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
	err := r.db.QueryRow(ctx, "SELECT item_code, name_key, type, rarity, icon, desc_key, max_stack, sources, effects, required_level FROM item_configs WHERE item_code = $1", itemCode).Scan(
		&cfg.ItemCode, &cfg.NameKey, &cfg.Type, &cfg.Rarity, &cfg.Icon, &cfg.DescKey, &cfg.MaxStack, &sourcesBytes, &effectsBytes, &cfg.RequiredLevel)
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
	rows, err := r.db.Query(ctx, "SELECT item_code, name_key, type, rarity, icon, desc_key, max_stack, sources, effects, required_level FROM item_configs")
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var result []*RepoItemConfig
	for rows.Next() {
		cfg := &RepoItemConfig{}
		var sourcesBytes, effectsBytes []byte
		err := rows.Scan(&cfg.ItemCode, &cfg.NameKey, &cfg.Type, &cfg.Rarity, &cfg.Icon, &cfg.DescKey, &cfg.MaxStack, &sourcesBytes, &effectsBytes, &cfg.RequiredLevel)
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
	case "RECRUIT_TICKET":
		heroCodes := strings.Split(codeParam, ",")
		for _, heroCode := range heroCodes {
			if heroCode == "" {
				continue
			}
			var skillsJSON string
			switch heroCode {
			case "DARK_ASSASSIN_01":
				skillsJSON = `[{"skill_code": "skill_shadow_strike", "level": 1}]`
			case "FIRE_GENERAL_01", "LIGHT_MAGE_01":
				skillsJSON = `[{"skill_code": "skill_fireball", "level": 1}]`
			case "WATER_TANK_01":
				skillsJSON = `[{"skill_code": "skill_shield", "level": 1}]`
			case "WOOD_HEALER_01":
				skillsJSON = `[{"skill_code": "skill_heal", "level": 1}]`
			default:
				skillsJSON = `[{"skill_code": "skill_slash", "level": 1}]`
			}
			_, err = tx.Exec(ctx, `
				INSERT INTO player_heroes (player_id, hero_code, traits, skills) 
				VALUES ($1, $2, '[]'::jsonb, $3::jsonb)`, playerID, heroCode, skillsJSON)
			if err != nil {
				return err
			}
		}
		_, err = tx.Exec(ctx, `
			INSERT INTO player_gacha_pity (player_id, banner_id, pull_count)
			VALUES ($1, 1, $2)
			ON CONFLICT (player_id, banner_id) DO UPDATE SET pull_count = EXCLUDED.pull_count`,
			playerID, valueParam)
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

func (r *PlayerRepository) GetSkillConfigs(ctx context.Context) ([]*SkillConfig, error) {
	rows, err := r.db.Query(ctx, "SELECT skill_code, name, damage_multiplier, cooldown, effect_type FROM skills")
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var result []*SkillConfig
	for rows.Next() {
		sc := &SkillConfig{}
		if err := rows.Scan(&sc.SkillCode, &sc.Name, &sc.DamageMultiplier, &sc.Cooldown, &sc.EffectType); err != nil {
			return nil, err
		}
		result = append(result, sc)
	}
	return result, rows.Err()
}

func (r *PlayerRepository) LevelUpHero(ctx context.Context, playerID int64, heroID int64) (*PlayerHero, error) {
	tx, err := r.db.Begin(ctx)
	if err != nil {
		return nil, err
	}
	defer tx.Rollback(ctx)

	// 1. Fetch hero info
	var h PlayerHero
	var traitsBytes, skillsBytes []byte
	err = tx.QueryRow(ctx, `
		SELECT ph.id, ph.player_id, ph.hero_code, ht.name, ht.rarity, ht.element, ht.role,
		       ph.level, ph.star, ph.exp, ht.base_hp, ht.base_atk, ht.base_def, ht.base_speed, ph.traits, ph.skills
		FROM player_heroes ph
		JOIN hero_templates ht ON ht.code = ph.hero_code
		WHERE ph.id = $1 AND ph.player_id = $2
	`, heroID, playerID).Scan(
		&h.ID, &h.PlayerID, &h.HeroCode, &h.Name, &h.Rarity, &h.Element, &h.Role,
		&h.Level, &h.Star, &h.Exp, &h.BaseHP, &h.BaseATK, &h.BaseDEF, &h.BaseSpeed, &traitsBytes, &skillsBytes,
	)
	if err != nil {
		return nil, err
	}
	h.Traits = string(traitsBytes)
	h.Skills = string(skillsBytes)

	// 2. Define cost based on current level (tier-based)
	var goldCost int64
	var woodCost int32
	var stoneCost int32

	currentLvl := h.Level
	if currentLvl < 10 {
		// Tier 1: 1-9
		goldCost = int64(currentLvl * 100)
		woodCost = currentLvl * 10
		stoneCost = 0
	} else if currentLvl < 20 {
		// Tier 2: 10-19
		goldCost = int64(currentLvl * 250)
		woodCost = currentLvl * 15
		stoneCost = (currentLvl - 9) * 5
	} else if currentLvl < 30 {
		// Tier 3: 20-29
		goldCost = int64(currentLvl * 500)
		woodCost = currentLvl * 25
		stoneCost = (currentLvl - 9) * 10
	} else {
		// Tier 4: 30+
		goldCost = int64(currentLvl * 1000)
		woodCost = currentLvl * 50
		stoneCost = (currentLvl - 9) * 20
	}

	// 3. Verify player gold
	var playerGold int64
	err = tx.QueryRow(ctx, "SELECT gold FROM players WHERE id = $1 FOR UPDATE", playerID).Scan(&playerGold)
	if err != nil {
		return nil, err
	}
	if playerGold < goldCost {
		return nil, fmt.Errorf("không đủ vàng (cần %d)", goldCost)
	}

	// 4. Verify player items (wood_1, stone_1)
	if woodCost > 0 {
		var woodQty int32
		err = tx.QueryRow(ctx, "SELECT quantity FROM user_items WHERE player_id = $1 AND item_code = '00003' FOR UPDATE", playerID).Scan(&woodQty)
		if err != nil {
			if errors.Is(err, pgx.ErrNoRows) {
				return nil, errors.New("không đủ Gỗ I")
			}
			return nil, err
		}
		if woodQty < woodCost {
			return nil, fmt.Errorf("không đủ Gỗ I (cần %d)", woodCost)
		}
	}

	if stoneCost > 0 {
		var stoneQty int32
		err = tx.QueryRow(ctx, "SELECT quantity FROM user_items WHERE player_id = $1 AND item_code = '00002' FOR UPDATE", playerID).Scan(&stoneQty)
		if err != nil {
			if errors.Is(err, pgx.ErrNoRows) {
				return nil, errors.New("không đủ Đá I")
			}
			return nil, err
		}
		if stoneQty < stoneCost {
			return nil, fmt.Errorf("không đủ Đá I (cần %d)", stoneCost)
		}
	}

	// 5. Deduct gold
	_, err = tx.Exec(ctx, "UPDATE players SET gold = gold - $1 WHERE id = $2", goldCost, playerID)
	if err != nil {
		return nil, err
	}

	// 6. Deduct materials
	if woodCost > 0 {
		_, err = tx.Exec(ctx, "UPDATE user_items SET quantity = quantity - $1 WHERE player_id = $2 AND item_code = '00003'", woodCost, playerID)
		if err != nil {
			return nil, err
		}
	}
	if stoneCost > 0 {
		_, err = tx.Exec(ctx, "UPDATE user_items SET quantity = quantity - $1 WHERE player_id = $2 AND item_code = '00002'", stoneCost, playerID)
		if err != nil {
			return nil, err
		}
	}

	// 7. Update hero level
	nextLvl := currentLvl + 1
	
	// Auto unlock skills based on new level
	var defaultSkills []string
	switch h.HeroCode {
	case "DARK_ASSASSIN_01":
		defaultSkills = []string{"skill_shadow_strike", "skill_slash", "skill_blood_claw"}
	case "FIRE_GENERAL_01":
		defaultSkills = []string{"skill_fireball", "skill_shield", "skill_meteor"}
	case "FIRE_WARRIOR_01":
		defaultSkills = []string{"skill_slash", "skill_shield", "skill_blade_tempest"}
	case "LIGHT_DEITY_01":
		defaultSkills = []string{"skill_slash", "skill_heal", "skill_divine_light"}
	case "LIGHT_MAGE_01":
		defaultSkills = []string{"skill_fireball", "skill_shield", "skill_lightning_storm"}
	case "WATER_TANK_01":
		defaultSkills = []string{"skill_shield", "skill_slash", "skill_frozen_armor"}
	case "WOOD_HEALER_01":
		defaultSkills = []string{"skill_heal", "skill_shield", "skill_holy_revive"}
	default:
		defaultSkills = []string{"skill_slash", "skill_shield", "skill_slash"}
	}

	var learnedSkills []map[string]interface{}
	if len(defaultSkills) > 0 && nextLvl >= 1 {
		learnedSkills = append(learnedSkills, map[string]interface{}{"skill_code": defaultSkills[0], "level": int32(1)})
	}
	if len(defaultSkills) > 1 && nextLvl >= 10 {
		learnedSkills = append(learnedSkills, map[string]interface{}{"skill_code": defaultSkills[1], "level": int32(1)})
	}
	if len(defaultSkills) > 2 && nextLvl >= 30 {
		learnedSkills = append(learnedSkills, map[string]interface{}{"skill_code": defaultSkills[2], "level": int32(1)})
	}

	skillsJSONBytes, _ := json.Marshal(learnedSkills)
	newSkillsJSON := string(skillsJSONBytes)

	err = tx.QueryRow(ctx, `
		UPDATE player_heroes SET level = $1, skills = $2
		WHERE id = $3 AND player_id = $4
		RETURNING level, skills
	`, nextLvl, newSkillsJSON, heroID, playerID).Scan(&h.Level, &skillsBytes)
	if err != nil {
		return nil, err
	}
	h.Skills = string(skillsBytes)

	err = tx.Commit(ctx)
	if err != nil {
		return nil, err
	}

	_ = r.UpdatePlayerPower(ctx, playerID)

	return &h, nil
}

func (r *PlayerRepository) ProcessPvECombatResult(ctx context.Context, playerID int64, stageID string, isVictory bool, rewardExp int32, rewardLinhThach int32) (*Player, error) {
	tx, err := r.db.Begin(ctx)
	if err != nil {
		return nil, err
	}
	defer tx.Rollback(ctx)

	// 1. Get current player stamina
	var stamina int32
	var maxStamina int32
	var lastStaminaAt time.Time
	err = tx.QueryRow(ctx, "SELECT stamina, max_stamina, last_stamina_at FROM players WHERE id = $1 FOR UPDATE", playerID).Scan(&stamina, &maxStamina, &lastStaminaAt)
	if err != nil {
		return nil, err
	}

	// Calculate stamina recovery first
	now := time.Now()
	if stamina < maxStamina {
		elapsed := now.Sub(lastStaminaAt)
		if elapsed >= 5*time.Minute {
			recovered := int32(elapsed.Seconds() / 300)
			if recovered > 0 {
				stamina += recovered
				if stamina >= maxStamina {
					stamina = maxStamina
					lastStaminaAt = now
				} else {
					lastStaminaAt = lastStaminaAt.Add(time.Duration(recovered) * 5 * time.Minute)
				}
			}
		}
	}

	// 2. Query stage config to get stamina cost (default to 5 if not found/empty) and reward drops
	staminaCost := int32(5)
	var jsonData string
	var stageConf struct {
		StaminaCost int32 `json:"staminaCost"`
		Rewards     []struct {
			ItemID string `json:"itemId"`
			Amount int    `json:"amount"`
		} `json:"rewards"`
	}
	err = tx.QueryRow(ctx, "SELECT json_data FROM stage_configs WHERE stage_id = $1", stageID).Scan(&jsonData)
	if err != nil || jsonData == "" {
		// Fallback stage rewards in JSON if database is missing configuration
		fallbackRewards := map[string]string{
			"stage_01": `{"staminaCost":5,"rewards":[{"itemId":"00000","amount":50},{"itemId":"00002","amount":5},{"itemId":"00003","amount":5}]}`,
			"stage_02": `{"staminaCost":5,"rewards":[{"itemId":"00000","amount":100},{"itemId":"00002","amount":10},{"itemId":"00003","amount":10}]}`,
			"stage_03": `{"staminaCost":5,"rewards":[{"itemId":"00000","amount":150},{"itemId":"00002","amount":15},{"itemId":"00003","amount":15}]}`,
			"stage_04": `{"staminaCost":5,"rewards":[{"itemId":"00000","amount":200},{"itemId":"00002","amount":20},{"itemId":"00003","amount":20}]}`,
			"stage_05": `{"staminaCost":5,"rewards":[{"itemId":"00000","amount":250},{"itemId":"00002","amount":25},{"itemId":"00003","amount":25}]}`,
			"stage_06": `{"staminaCost":5,"rewards":[{"itemId":"00000","amount":300},{"itemId":"00002","amount":30},{"itemId":"00003","amount":30}]}`,
			"stage_07": `{"staminaCost":5,"rewards":[{"itemId":"00000","amount":350},{"itemId":"00002","amount":35},{"itemId":"00003","amount":35}]}`,
			"stage_08": `{"staminaCost":5,"rewards":[{"itemId":"00000","amount":400},{"itemId":"00002","amount":40},{"itemId":"00003","amount":40}]}`,
			"stage_09": `{"staminaCost":5,"rewards":[{"itemId":"00000","amount":450},{"itemId":"00002","amount":45},{"itemId":"00003","amount":45}]}`,
			"stage_10": `{"staminaCost":5,"rewards":[{"itemId":"00000","amount":500},{"itemId":"00002","amount":50},{"itemId":"00003","amount":50}]}`,
			"Stage_Fallback": `{"staminaCost":5,"rewards":[{"itemId":"00000","amount":20}]}`,
		}

		lowerStageID := strings.ToLower(stageID)
		if val, found := fallbackRewards[stageID]; found {
			jsonData = val
		} else if val, found := fallbackRewards[lowerStageID]; found {
			jsonData = val
		} else {
			// Chung cho các ải khác
			jsonData = `{"staminaCost":5,"rewards":[{"itemId":"00000","amount":10},{"itemId":"00002","amount":5},{"itemId":"00003","amount":5}]}`
		}
		
		// Lưu vào DB luôn để lần sau truy vấn nhanh
		_, _ = tx.Exec(ctx, "INSERT INTO stage_configs (stage_id, json_data) VALUES ($1, $2) ON CONFLICT (stage_id) DO NOTHING", stageID, jsonData)
	}

	if jsonData != "" {
		if json.Unmarshal([]byte(jsonData), &stageConf) == nil {
			if stageConf.StaminaCost > 0 {
				staminaCost = stageConf.StaminaCost
			}
		}
	}

	// 3. Deduct stamina (ensure we don't go below 0)
	if stamina < staminaCost {
		stamina = 0
	} else {
		if stamina == maxStamina {
			lastStaminaAt = now
		}
		stamina -= staminaCost
	}

	// 4. If victory, add rewards!
	goldDelta := int64(0)
	expDelta := int64(0)
	if isVictory {
		goldDelta = int64(rewardLinhThach)
		expDelta = int64(rewardExp)

		// Record stage progress on server DB!
		_, err = tx.Exec(ctx, "INSERT INTO player_stages (player_id, stage_id) VALUES ($1, $2) ON CONFLICT (player_id, stage_id) DO NOTHING", playerID, stageID)
		if err != nil {
			return nil, err
		}

		// Reward items
		for _, reward := range stageConf.Rewards {
			if reward.ItemID != "" && reward.Amount > 0 {
				var exists bool
				err = tx.QueryRow(ctx, "SELECT EXISTS(SELECT 1 FROM user_items WHERE player_id = $1 AND item_code = $2)", playerID, reward.ItemID).Scan(&exists)
				if err != nil {
					return nil, err
				}
				if exists {
					_, err = tx.Exec(ctx, "UPDATE user_items SET quantity = quantity + $1, updated_at = NOW() WHERE player_id = $2 AND item_code = $3", reward.Amount, playerID, reward.ItemID)
				} else {
					_, err = tx.Exec(ctx, "INSERT INTO user_items (player_id, item_code, quantity, stats) VALUES ($1, $2, $3, '[]')", playerID, reward.ItemID, reward.Amount)
				}
				if err != nil {
					return nil, err
				}
			}
		}
	}

	// 5. Update players table
	q := `
		UPDATE players
		SET stamina = $2,
		    last_stamina_at = $5,
		    gold = gold + $3,
		    exp = exp + $4,
		    updated_at = NOW()
		WHERE id = $1
		RETURNING id, user_id, nickname, level, exp, gold, diamond, stamina, max_stamina, last_stamina_at, power, created_at, updated_at
	`
	p := &Player{}
	err = tx.QueryRow(ctx, q, playerID, stamina, goldDelta, expDelta, lastStaminaAt).Scan(
		&p.ID, &p.UserID, &p.Nickname, &p.Level, &p.Exp, &p.Gold, &p.Diamond,
		&p.Stamina, &p.MaxStamina, &p.LastStaminaAt, &p.Power, &p.CreatedAt, &p.UpdatedAt,
	)
	if err != nil {
		return nil, err
	}

	// 6. Commit transaction
	if err := tx.Commit(ctx); err != nil {
		return nil, err
	}

	return p, nil
}

func (r *PlayerRepository) GetCompletedStages(ctx context.Context, playerID int64) ([]string, error) {
	q := `SELECT stage_id FROM player_stages WHERE player_id = $1`
	rows, err := r.db.Query(ctx, q, playerID)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	var stages []string
	for rows.Next() {
		var stageID string
		if err := rows.Scan(&stageID); err != nil {
			return nil, err
		}
		stages = append(stages, stageID)
	}
	return stages, nil
}

func (r *PlayerRepository) GetLeaderboard(ctx context.Context, leaderboardType string) ([]*LeaderboardRecord, error) {
	var q string
	if leaderboardType == "power" {
		q = `
			SELECT ROW_NUMBER() OVER(ORDER BY power DESC) AS rank, nickname, power AS value
			FROM players
			ORDER BY power DESC
			LIMIT 50
		`
	} else {
		q = `
			SELECT ROW_NUMBER() OVER(ORDER BY COUNT(ps.stage_id) DESC) AS rank, p.nickname, COUNT(ps.stage_id) AS value
			FROM players p
			LEFT JOIN player_stages ps ON ps.player_id = p.id
			GROUP BY p.id, p.nickname
			ORDER BY value DESC
			LIMIT 50
		`
	}
	rows, err := r.db.Query(ctx, q)
	if err != nil {
		return nil, err
	}
	defer rows.Close()
	var records []*LeaderboardRecord
	for rows.Next() {
		rec := &LeaderboardRecord{}
		if err := rows.Scan(&rec.Rank, &rec.Nickname, &rec.Value); err != nil {
			return nil, err
		}
		records = append(records, rec)
	}
	return records, nil
}

func (r *PlayerRepository) UpdatePlayerPower(ctx context.Context, playerID int64) error {
	q := `
		UPDATE players p
		SET power = COALESCE((
			SELECT SUM((ht.base_atk + ph.level * 10) * 10 + (ht.base_hp + ph.level * 100) + (ht.base_def + ph.level * 5) * 5)
			FROM player_heroes ph
			JOIN hero_templates ht ON ht.code = ph.hero_code
			WHERE ph.player_id = p.id
		), 0)
		WHERE p.id = $1
	`
	_, err := r.db.Exec(ctx, q, playerID)
	return err
}

func (r *PlayerRepository) CollectResource(ctx context.Context, playerID int64, instanceID int64, itemCode string, amount int64) error {
	tx, err := r.db.Begin(ctx)
	if err != nil {
		return err
	}
	defer tx.Rollback(ctx)

	_, err = tx.Exec(ctx, "UPDATE player_buildings SET last_collect_at = NOW() WHERE player_id = $1 AND id = $2", playerID, instanceID)
	if err != nil {
		return err
	}

	var exists bool
	err = tx.QueryRow(ctx, "SELECT EXISTS(SELECT 1 FROM user_items WHERE player_id = $1 AND item_code = $2)", playerID, itemCode).Scan(&exists)
	if err != nil {
		return err
	}

	if exists {
		_, err = tx.Exec(ctx, "UPDATE user_items SET quantity = quantity + $1, updated_at = NOW() WHERE player_id = $2 AND item_code = $3", amount, playerID, itemCode)
	} else {
		_, err = tx.Exec(ctx, "INSERT INTO user_items (player_id, item_code, quantity, stats) VALUES ($1, $2, $3, '[]')", playerID, itemCode, amount)
	}
	return tx.Commit(ctx)
}

func (r *PlayerRepository) GetShopItem(ctx context.Context, shopItemID string) (*ShopItem, error) {
	q := "SELECT id, shop_item_id, shop_type, item_code, amount, original_price, is_discountable FROM shop_items WHERE shop_item_id = $1"
	row := r.db.QueryRow(ctx, q, shopItemID)
	item := &ShopItem{}
	var priceBytes []byte
	err := row.Scan(&item.ID, &item.ShopItemID, &item.ShopType, &item.ItemCode, &item.Amount, &priceBytes, &item.IsDiscountable)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, ErrNotFound
		}
		return nil, err
	}
	item.OriginalPrice = string(priceBytes)
	return item, nil
}

func (r *PlayerRepository) GetShopItemsByType(ctx context.Context, shopType string) ([]*ShopItem, error) {
	q := "SELECT id, shop_item_id, shop_type, item_code, amount, original_price, is_discountable FROM shop_items WHERE shop_type = $1"
	rows, err := r.db.Query(ctx, q, shopType)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var list []*ShopItem
	for rows.Next() {
		item := &ShopItem{}
		var priceBytes []byte
		err := rows.Scan(&item.ID, &item.ShopItemID, &item.ShopType, &item.ItemCode, &item.Amount, &priceBytes, &item.IsDiscountable)
		if err != nil {
			return nil, err
		}
		item.OriginalPrice = string(priceBytes)
		list = append(list, item)
	}
	return list, nil
}

func (r *PlayerRepository) GetAllShopItems(ctx context.Context) ([]*ShopItem, error) {
	q := "SELECT id, shop_item_id, shop_type, item_code, amount, original_price, is_discountable FROM shop_items"
	rows, err := r.db.Query(ctx, q)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var list []*ShopItem
	for rows.Next() {
		item := &ShopItem{}
		var priceBytes []byte
		err := rows.Scan(&item.ID, &item.ShopItemID, &item.ShopType, &item.ItemCode, &item.Amount, &priceBytes, &item.IsDiscountable)
		if err != nil {
			return nil, err
		}
		item.OriginalPrice = string(priceBytes)
		list = append(list, item)
	}
	return list, nil
}

func (r *PlayerRepository) InsertShopItem(ctx context.Context, item *ShopItem) error {
	q := `
		INSERT INTO shop_items (shop_item_id, shop_type, item_code, amount, original_price, is_discountable)
		VALUES ($1, $2, $3, $4, $5, $6)
		ON CONFLICT (shop_item_id) DO UPDATE
		SET shop_type = EXCLUDED.shop_type,
		    item_code = EXCLUDED.item_code,
		    amount = EXCLUDED.amount,
		    original_price = EXCLUDED.original_price,
		    is_discountable = EXCLUDED.is_discountable
	`
	_, err := r.db.Exec(ctx, q, item.ShopItemID, item.ShopType, item.ItemCode, item.Amount, item.OriginalPrice, item.IsDiscountable)
	return err
}

func (r *PlayerRepository) DeleteShopItem(ctx context.Context, shopItemID string) error {
	q := "DELETE FROM shop_items WHERE shop_item_id = $1"
	_, err := r.db.Exec(ctx, q, shopItemID)
	return err
}

func (r *PlayerRepository) GetPlayerShopItems(ctx context.Context, playerID int64, shopType string) ([]*PlayerShopItemInstance, time.Time, error) {
	// 1. Get next refresh time
	var nextRefresh time.Time
	err := r.db.QueryRow(ctx, "SELECT next_refresh_at FROM player_shop_resets WHERE player_id = $1 AND shop_type = $2", playerID, shopType).Scan(&nextRefresh)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, time.Time{}, nil // Triggers refresh
		}
		return nil, time.Time{}, err
	}

	// 2. Get active items
	q := "SELECT id, player_id, shop_type, shop_item_id, item_code, amount, final_price, discount_pct, is_bought FROM player_shops WHERE player_id = $1 AND shop_type = $2"
	rows, err := r.db.Query(ctx, q, playerID, shopType)
	if err != nil {
		return nil, nextRefresh, err
	}
	defer rows.Close()

	var list []*PlayerShopItemInstance
	for rows.Next() {
		inst := &PlayerShopItemInstance{}
		var priceBytes []byte
		err := rows.Scan(&inst.ID, &inst.PlayerID, &inst.ShopType, &inst.ShopItemID, &inst.ItemCode, &inst.Amount, &priceBytes, &inst.DiscountPct, &inst.IsBought)
		if err != nil {
			return nil, nextRefresh, err
		}
		inst.FinalPrice = string(priceBytes)
		list = append(list, inst)
	}
	return list, nextRefresh, nil
}

func (r *PlayerRepository) SavePlayerShopItems(ctx context.Context, playerID int64, shopType string, items []*PlayerShopItemInstance, nextRefresh time.Time) error {
	tx, err := r.db.Begin(ctx)
	if err != nil {
		return err
	}
	defer tx.Rollback(ctx)

	// Clean up old items
	_, err = tx.Exec(ctx, "DELETE FROM player_shops WHERE player_id = $1 AND shop_type = $2", playerID, shopType)
	if err != nil {
		return err
	}

	// Insert new items
	for _, item := range items {
		q := `
			INSERT INTO player_shops (player_id, shop_type, shop_item_id, item_code, amount, final_price, discount_pct, is_bought)
			VALUES ($1, $2, $3, $4, $5, $6, $7, $8)
		`
		_, err = tx.Exec(ctx, q, playerID, shopType, item.ShopItemID, item.ItemCode, item.Amount, item.FinalPrice, item.DiscountPct, item.IsBought)
		if err != nil {
			return err
		}
	}

	// Update next refresh time
	qReset := `
		INSERT INTO player_shop_resets (player_id, shop_type, next_refresh_at)
		VALUES ($1, $2, $3)
		ON CONFLICT (player_id, shop_type) DO UPDATE
		SET next_refresh_at = EXCLUDED.next_refresh_at
	`
	_, err = tx.Exec(ctx, qReset, playerID, shopType, nextRefresh)
	if err != nil {
		return err
	}

	return tx.Commit(ctx)
}

func (r *PlayerRepository) BuyPlayerShopItemTransaction(ctx context.Context, playerID int64, instanceID int64, quantity int32) ([]*UserItem, error) {
	tx, err := r.db.Begin(ctx)
	if err != nil {
		return nil, err
	}
	defer tx.Rollback(ctx)

	// 1. Get player shop item instance details
	var itemCode string
	var amount int32
	var finalPriceBytes []byte
	var isBought bool
	err = tx.QueryRow(ctx, "SELECT item_code, amount, final_price, is_bought FROM player_shops WHERE id = $1 AND player_id = $2 FOR UPDATE", instanceID, playerID).Scan(&itemCode, &amount, &finalPriceBytes, &isBought)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, errors.New("sản phẩm không tồn tại")
		}
		return nil, err
	}

	if isBought {
		return nil, errors.New("sản phẩm đã được mua rồi")
	}

	var costs []ShopCost
	if errJson := json.Unmarshal(finalPriceBytes, &costs); errJson != nil {
		return nil, errJson
	}

	// 2. Lock & check currencies/items
	for _, cost := range costs {
		totalCost := int64(cost.Amount) * int64(quantity)
		if cost.ItemCode == "gold" || cost.ItemCode == "00001" {
			var currentGold int64
			err = tx.QueryRow(ctx, "SELECT gold FROM players WHERE id = $1 FOR UPDATE", playerID).Scan(&currentGold)
			if err != nil {
				return nil, err
			}
			if currentGold < totalCost {
				return nil, errors.New("không đủ vàng")
			}
			_, err = tx.Exec(ctx, "UPDATE players SET gold = gold - $1 WHERE id = $2", totalCost, playerID)
			if err != nil {
				return nil, err
			}
		} else if cost.ItemCode == "diamond" || cost.ItemCode == "00000" {
			var currentDiamond int64
			err = tx.QueryRow(ctx, "SELECT diamond FROM players WHERE id = $1 FOR UPDATE", playerID).Scan(&currentDiamond)
			if err != nil {
				return nil, err
			}
			if currentDiamond < totalCost {
				return nil, errors.New("không đủ xu/kim cương")
			}
			_, err = tx.Exec(ctx, "UPDATE players SET diamond = diamond - $1 WHERE id = $2", totalCost, playerID)
			if err != nil {
				return nil, err
			}
		} else if cost.ItemCode == "stamina" {
			var currentStamina int32
			err = tx.QueryRow(ctx, "SELECT stamina FROM players WHERE id = $1 FOR UPDATE", playerID).Scan(&currentStamina)
			if err != nil {
				return nil, err
			}
			if int64(currentStamina) < totalCost {
				return nil, errors.New("không đủ thể lực")
			}
			_, err = tx.Exec(ctx, "UPDATE players SET stamina = stamina - $1 WHERE id = $2", totalCost, playerID)
			if err != nil {
				return nil, err
			}
		} else {
			// It is an inventory item! Check from user_items
			var currentQty int32
			err = tx.QueryRow(ctx, "SELECT quantity FROM user_items WHERE player_id = $1 AND item_code = $2 FOR UPDATE", playerID, cost.ItemCode).Scan(&currentQty)
			if err != nil {
				if errors.Is(err, pgx.ErrNoRows) {
					return nil, fmt.Errorf("không có vật phẩm %s trong túi", cost.ItemCode)
				}
				return nil, err
			}
			if int64(currentQty) < totalCost {
				return nil, fmt.Errorf("không đủ vật phẩm %s", cost.ItemCode)
			}

			// Deduct items
			if int64(currentQty) == totalCost {
				_, err = tx.Exec(ctx, "DELETE FROM user_items WHERE player_id = $1 AND item_code = $2", playerID, cost.ItemCode)
			} else {
				_, err = tx.Exec(ctx, "UPDATE user_items SET quantity = quantity - $1 WHERE player_id = $2 AND item_code = $3", totalCost, playerID, cost.ItemCode)
			}
			if err != nil {
				return nil, err
			}
		}
	}

	// 3. Mark as bought
	_, err = tx.Exec(ctx, "UPDATE player_shops SET is_bought = TRUE WHERE id = $1", instanceID)
	if err != nil {
		return nil, err
	}

	// 4. Grant reward item to player
	gainedQty := amount * quantity
	var exists bool
	err = tx.QueryRow(ctx, "SELECT EXISTS(SELECT 1 FROM user_items WHERE player_id = $1 AND item_code = $2)", playerID, itemCode).Scan(&exists)
	if err != nil {
		return nil, err
	}

	if exists {
		_, err = tx.Exec(ctx, "UPDATE user_items SET quantity = quantity + $1, updated_at = NOW() WHERE player_id = $2 AND item_code = $3", gainedQty, playerID, itemCode)
	} else {
		_, err = tx.Exec(ctx, "INSERT INTO user_items (player_id, item_code, quantity, stats) VALUES ($1, $2, $3, '[]')", playerID, itemCode, gainedQty)
	}
	if err != nil {
		return nil, err
	}

	// Commit
	err = tx.Commit(ctx)
	if err != nil {
		return nil, err
	}

	// Retrieve user's full inventory of the gained item type to return updated quantities
	var returnedItems []*UserItem
	rows, err := r.db.Query(ctx, "SELECT id, player_id, item_code, quantity, stats FROM user_items WHERE player_id = $1", playerID)
	if err == nil {
		defer rows.Close()
		for rows.Next() {
			item := &UserItem{}
			var statsBytes []byte
			if errScan := rows.Scan(&item.ID, &item.PlayerID, &item.ItemCode, &item.Quantity, &statsBytes); errScan == nil {
				item.Stats = string(statsBytes)
				returnedItems = append(returnedItems, item)
			}
		}
	}

	return returnedItems, nil
}




