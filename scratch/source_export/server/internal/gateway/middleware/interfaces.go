package middleware

import (
	"context"
	"time"

	"github.com/redis/go-redis/v9"
)

type IRedisClient interface {
	Get(ctx context.Context, key string) *redis.StringCmd
	Set(ctx context.Context, key string, value interface{}, expiration time.Duration) *redis.StatusCmd
}
