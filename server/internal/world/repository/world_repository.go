package repository

import (
	"context"
	"errors"
	"time"

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
		result = append(result, b)
	}
	return result, nil
}

func (r *PlayerRepository) UpgradeBuilding(ctx context.Context, playerID int64, code string, endAt time.Time) (*PlayerBuilding, error) {
	q := `
		UPDATE player_buildings SET level = level + 1, upgrade_end_at = $3
		WHERE player_id = $1 AND building_code = $2 AND upgrade_end_at IS NULL
		RETURNING id, player_id, building_code, level, upgrade_end_at, last_collect_at
	`
	b := &PlayerBuilding{BuildingCode: code}
	err := r.db.QueryRow(ctx, q, playerID, code, endAt).Scan(
		&b.ID, &b.PlayerID, &b.BuildingCode, &b.Level, &b.UpgradeEndAt, &b.LastCollectAt,
	)
	if errors.Is(err, pgx.ErrNoRows) {
		return nil, ErrNotFound
	}
	return b, err
}

func (r *PlayerRepository) CollectGold(ctx context.Context, playerID int64, code string) (int64, error) {
	q := `
		UPDATE player_buildings
		SET last_collect_at = NOW()
		WHERE player_id = $1 AND building_code = $2
		RETURNING level, last_collect_at,
		          EXTRACT(EPOCH FROM (NOW() - last_collect_at)) / 3600 AS hours
	`
	var level int32
	var lastCollect time.Time
	var hours float64
	err := r.db.QueryRow(ctx, q, playerID, code).Scan(&level, &lastCollect, &hours)
	if err != nil {
		return 0, err
	}
	if hours > 12 {
		hours = 12
	}
	gold := int64(float64(level) * 10 * hours)
	return gold, nil
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
