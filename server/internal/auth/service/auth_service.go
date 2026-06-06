package service

import (
	"context"
	"strconv"
	"time"

	"github.com/redis/go-redis/v9"
	"server/internal/auth/repository"
	"server/pkg/jwtutil"
)

type AuthService struct {
	userRepo repository.IUserRepository
	zoneRepo repository.IZoneRepository
	jwt      *jwtutil.Manager
	redis    *redis.Client
}

func New(
	userRepo repository.IUserRepository,
	zoneRepo repository.IZoneRepository,
	jwt *jwtutil.Manager,
	redis *redis.Client,
) *AuthService {
	return &AuthService{
		userRepo: userRepo,
		zoneRepo: zoneRepo,
		jwt:      jwt,
		redis:    redis,
	}
}

func (s *AuthService) sessionKey(userID int64) string {
	return "session:" + strconv.FormatInt(userID, 10)
}

func (s *AuthService) SetActiveSession(ctx context.Context, userID int64, token string, expiration time.Duration) error {
	if s.redis == nil {
		return nil // Skip if redis is not configured (e.g. in tests)
	}
	return s.redis.Set(ctx, s.sessionKey(userID), token, expiration).Err()
}
