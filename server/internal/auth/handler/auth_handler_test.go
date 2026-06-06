package handler

import (
	"context"
	"testing"

	"go.uber.org/zap"
	"server/internal/auth/repository"
	"server/internal/auth/service"
	"server/pkg/jwtutil"
	pb "server/pkg/pb"
)

func TestAuthHandler(t *testing.T) {
	userRepo := &repository.MockUserRepo{Users: make(map[string]*repository.User)}
	zoneRepo := &repository.MockZoneRepo{Zones: []*repository.Zone{{ID: 1, Name: "Zone 1", Status: "Online"}}}
	jwt := jwtutil.New("test_secret", 1)
	svc := service.New(userRepo, zoneRepo, jwt, nil)
	log := zap.NewNop()
	h := New(svc, log)

	ctx := context.Background()

	t.Run("RegisterAndLogin", func(t *testing.T) {
		regResp, err := h.Register(ctx, &pb.RegisterRequest{
			Username: "handleruser",
			Email:    "handler@example.com",
			Password: "password123",
		})
		if err != nil {
			t.Fatalf("Register failed: %v", err)
		}
		if regResp.Code != 0 {
			t.Errorf("Expected reg code 0, got %d", regResp.Code)
		}

		loginResp, err := h.Login(ctx, &pb.LoginRequest{
			Username: "handleruser",
			Password: "password123",
		})
		if err != nil {
			t.Fatalf("Login failed: %v", err)
		}
		if loginResp.Code != 0 {
			t.Errorf("Expected login code 0, got %d", loginResp.Code)
		}
		if loginResp.Token == "" {
			t.Error("Expected token in login response")
		}
		if loginResp.ZoneList == nil || len(loginResp.ZoneList.Zones) == 0 {
			t.Error("Expected zone list in login response")
		}
	})

	t.Run("LoginFailure", func(t *testing.T) {
		loginResp, err := h.Login(ctx, &pb.LoginRequest{
			Username: "wronguser",
			Password: "password123",
		})
		if err != nil {
			t.Fatalf("Login failed: %v", err)
		}
		if loginResp.Code == 0 {
			t.Error("Expected failure code for wrong user, got 0")
		}
	})
}
