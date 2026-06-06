package repository

import (
	"context"
)

type IUserRepository interface {
	Create(ctx context.Context, username, email, passwordHash string) (*User, error)
	FindByEmail(ctx context.Context, email string) (*User, error)
	FindByUsername(ctx context.Context, username string) (*User, error)
}

type IZoneRepository interface {
	FindActive(ctx context.Context) ([]*Zone, error)
}
