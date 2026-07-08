package database

import (
	"context"
	"fmt"

	"github.com/redis/go-redis/v9"
	"go.uber.org/zap"
)

func NewRedisClient(addr, password string, db int, log *zap.Logger) (*redis.Client, error) {
	rdb := redis.NewClient(&redis.Options{
		Addr:     addr,
		Password: password,
		DB:       db,
	})

	if err := rdb.Ping(context.Background()).Err(); err != nil {
		return nil, fmt.Errorf("connect to redis: %w", err)
	}

	log.Info("Connected to Redis", zap.String("addr", addr))
	return rdb, nil
}
