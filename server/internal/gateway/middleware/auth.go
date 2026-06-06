package middleware

import (
	"context"
	"strings"

	"go.uber.org/zap"
	"google.golang.org/grpc"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/metadata"
	"google.golang.org/grpc/status"

	"server/pkg/jwtutil"
	"github.com/redis/go-redis/v9"
	"fmt"
)

type contextKey string

const (
	CtxUserID contextKey = "user_id"
	CtxEmail  contextKey = "email"
)

func AuthInterceptor(jwtMgr *jwtutil.Manager, rdb IRedisClient, log *zap.Logger) grpc.UnaryServerInterceptor {
	return func(
		ctx context.Context,
		req interface{},
		info *grpc.UnaryServerInfo,
		handler grpc.UnaryHandler,
	) (interface{}, error) {
		if info.FullMethod == "/pb.GatewayService/SaveAdminMap" {
			return handler(ctx, req)
		}

		md, ok := metadata.FromIncomingContext(ctx)
		if !ok {
			return nil, status.Error(codes.Unauthenticated, "missing metadata")
		}

		authValues := md["authorization"]
		if len(authValues) == 0 {
			return nil, status.Error(codes.Unauthenticated, "missing authorization token")
		}

		tokenStr := strings.TrimPrefix(authValues[0], "Bearer ")
		if tokenStr == authValues[0] {
			tokenStr = authValues[0]
		}

		claims, err := jwtMgr.Verify(tokenStr)
		if err != nil {
			log.Warn("Invalid JWT token", zap.String("method", info.FullMethod), zap.Error(err))
			return nil, status.Error(codes.Unauthenticated, "invalid or expired token")
		}

		sessionKey := fmt.Sprintf("session:%d", claims.UserID)
		activeToken, err := rdb.Get(ctx, sessionKey).Result()
		if err != nil {
			if err == redis.Nil {
				return nil, status.Error(codes.Unauthenticated, "session not found or expired")
			}
			log.Error("Redis error in auth middleware", zap.Error(err))
			return nil, status.Error(codes.Internal, "internal session error")
		}

		if activeToken != tokenStr {
			log.Warn("User session kicked", zap.Int64("user_id", claims.UserID))
			return nil, status.Error(codes.Unauthenticated, "account logged in from another device")
		}

		ctx = context.WithValue(ctx, CtxUserID, claims.UserID)
		ctx = context.WithValue(ctx, CtxEmail, claims.Email)

		return handler(ctx, req)
	}
}

func GetUserID(ctx context.Context) (int64, bool) {
	v := ctx.Value(CtxUserID)
	if v == nil {
		return 0, false
	}
	id, ok := v.(int64)
	return id, ok
}
