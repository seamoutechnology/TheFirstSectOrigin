package middleware

import (
	"context"
	"testing"

	"go.uber.org/zap"
	"google.golang.org/grpc"
	"google.golang.org/grpc/metadata"
	"server/pkg/jwtutil"
	"github.com/redis/go-redis/v9"
)

type MockRedis struct {
	IRedisClient
	Data map[string]string
}

func (m *MockRedis) Get(ctx context.Context, key string) *redis.StringCmd {
	cmd := redis.NewStringCmd(ctx)
	if val, ok := m.Data[key]; ok {
		cmd.SetVal(val)
	} else {
		cmd.SetErr(redis.Nil)
	}
	return cmd
}

func TestAuthInterceptor(t *testing.T) {
	jwtMgr := jwtutil.New("test_secret", 1)
	log := zap.NewNop()
	
	userID := int64(123)
	email := "test@example.com"
	token, _ := jwtMgr.Generate(userID, email)
	
	mockRedis := &MockRedis{Data: map[string]string{
		"session:123": token,
	}}

	interceptor := AuthInterceptor(jwtMgr, mockRedis, log)

	dummyHandler := func(ctx context.Context, req interface{}) (interface{}, error) {
		return "ok", nil
	}

	t.Run("Success", func(t *testing.T) {
		md := metadata.Pairs("authorization", "Bearer "+token)
		ctx := metadata.NewIncomingContext(context.Background(), md)
		
		handler := func(ctx context.Context, req interface{}) (interface{}, error) {
			uid, ok := GetUserID(ctx)
			if !ok || uid != userID {
				t.Errorf("Expected userID %d in context, got %d", userID, uid)
			}
			return "ok", nil
		}

		resp, err := interceptor(ctx, nil, &grpc.UnaryServerInfo{}, handler)
		if err != nil {
			t.Fatalf("Interceptor failed: %v", err)
		}
		if resp != "ok" {
			t.Errorf("Expected resp ok, got %v", resp)
		}
	})

	t.Run("MissingMetadata", func(t *testing.T) {
		_, err := interceptor(context.Background(), nil, &grpc.UnaryServerInfo{}, dummyHandler)
		if err == nil {
			t.Fatal("Expected error for missing metadata")
		}
	})

	t.Run("InvalidToken", func(t *testing.T) {
		md := metadata.Pairs("authorization", "Bearer invalid")
		ctx := metadata.NewIncomingContext(context.Background(), md)
		_, err := interceptor(ctx, nil, &grpc.UnaryServerInfo{}, dummyHandler)
		if err == nil {
			t.Fatal("Expected error for invalid token")
		}
	})

	t.Run("SessionMismatch", func(t *testing.T) {
		otherToken, _ := jwtMgr.Generate(userID, "other@example.com")
		
		md := metadata.Pairs("authorization", "Bearer "+otherToken)
		ctx := metadata.NewIncomingContext(context.Background(), md)
		
		_, err := interceptor(ctx, nil, &grpc.UnaryServerInfo{}, dummyHandler)
		if err == nil {
			t.Fatal("Expected error for session mismatch")
		}
	})
}
