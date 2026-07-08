package repository

import (
	"context"
	"errors"
	"time"

	"github.com/jackc/pgx/v5"
	"github.com/jackc/pgx/v5/pgxpool"
)

type User struct {
	ID           int64
	Username     string
	Email        string
	PasswordHash string
	IsBanned     bool
	BanReason    *string
	CreatedAt    time.Time
	UpdatedAt    time.Time
}

type Zone struct {
	ID          int
	Name        string
	Status      string
	GatewayURL  string
	IsActive    bool
	CPUUsage    float64
	RAMUsage    float64
	PlayerCount int
	MaxPlayers  int
}

var ErrNotFound = errors.New("record not found")

type UserRepository struct {
	db *pgxpool.Pool
}

func NewUserRepository(db *pgxpool.Pool) *UserRepository {
	return &UserRepository{db: db}
}

func (r *UserRepository) Create(ctx context.Context, username, email, passwordHash string) (*User, error) {
	query := `
		INSERT INTO users (username, email, password_hash)
		VALUES ($1, $2, $3)
		RETURNING id, username, email, password_hash, is_banned, created_at, updated_at
	`
	user := &User{}
	err := r.db.QueryRow(ctx, query, username, email, passwordHash).Scan(
		&user.ID, &user.Username, &user.Email, &user.PasswordHash,
		&user.IsBanned, &user.CreatedAt, &user.UpdatedAt,
	)
	if err != nil {
		return nil, err
	}
	return user, nil
}

func (r *UserRepository) FindByEmail(ctx context.Context, email string) (*User, error) {
	query := `
		SELECT id, username, email, password_hash, is_banned, ban_reason, created_at, updated_at
		FROM users WHERE email = $1
	`
	user := &User{}
	err := r.db.QueryRow(ctx, query, email).Scan(
		&user.ID, &user.Username, &user.Email, &user.PasswordHash,
		&user.IsBanned, &user.BanReason, &user.CreatedAt, &user.UpdatedAt,
	)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, ErrNotFound
		}
		return nil, err
	}
	return user, nil
}

func (r *UserRepository) FindByUsername(ctx context.Context, username string) (*User, error) {
	query := `
		SELECT id, username, email, password_hash, is_banned, ban_reason, created_at, updated_at
		FROM users WHERE username = $1
	`
	user := &User{}
	err := r.db.QueryRow(ctx, query, username).Scan(
		&user.ID, &user.Username, &user.Email, &user.PasswordHash,
		&user.IsBanned, &user.BanReason, &user.CreatedAt, &user.UpdatedAt,
	)
	if err != nil {
		if errors.Is(err, pgx.ErrNoRows) {
			return nil, ErrNotFound
		}
		return nil, err
	}
	return user, nil
}

type ZoneRepository struct {
	db *pgxpool.Pool
}

func NewZoneRepository(db *pgxpool.Pool) *ZoneRepository {
	return &ZoneRepository{db: db}
}

func (r *ZoneRepository) FindActive(ctx context.Context) ([]*Zone, error) {
	query := `SELECT id, name, status, gateway_url, cpu_usage, ram_usage, player_count, max_players FROM zones WHERE is_active = TRUE ORDER BY id`
	rows, err := r.db.Query(ctx, query)
	if err != nil {
		return nil, err
	}
	defer rows.Close()

	var zones []*Zone
	for rows.Next() {
		z := &Zone{}
		if err := rows.Scan(&z.ID, &z.Name, &z.Status, &z.GatewayURL, &z.CPUUsage, &z.RAMUsage, &z.PlayerCount, &z.MaxPlayers); err != nil {
			return nil, err
		}
		zones = append(zones, z)
	}
	return zones, nil
}

func (r *ZoneRepository) UpdateMetrics(ctx context.Context, zoneID int, cpu, ram float64, players int) error {
	query := `UPDATE zones SET cpu_usage = $2, ram_usage = $3, player_count = $4 WHERE id = $1`
	_, err := r.db.Exec(ctx, query, zoneID, cpu, ram, players)
	return err
}

func (r *ZoneRepository) CreateZone(ctx context.Context, name, status, gatewayURL string) error {
	query := `INSERT INTO zones (name, status, gateway_url) VALUES ($1, $2, $3)`
	_, err := r.db.Exec(ctx, query, name, status, gatewayURL)
	return err
}

func (r *ZoneRepository) GetCount(ctx context.Context) (int, error) {
	query := `SELECT COUNT(*) FROM zones WHERE is_active = TRUE`
	var count int
	err := r.db.QueryRow(ctx, query).Scan(&count)
	return count, err
}
