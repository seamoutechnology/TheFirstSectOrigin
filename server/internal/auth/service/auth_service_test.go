package service

import (
	"context"
	"testing"

	"server/internal/auth/repository"
	"server/pkg/jwtutil"
)

func TestAuthService_Register(t *testing.T) {
	userRepo := &repository.MockUserRepo{Users: make(map[string]*repository.User)}
	jwt := jwtutil.New("test_secret", 1)
	svc := New(userRepo, nil, jwt, nil)

	ctx := context.Background()

	t.Run("Success", func(t *testing.T) {
		res, err := svc.Register(ctx, "testuser", "test@example.com", "password123")
		if err != nil {
			t.Fatalf("Register failed: %v", err)
		}
		if res.Code != ErrCodeSuccess {
			t.Errorf("Expected success code, got %d", res.Code)
		}
	})

	t.Run("DuplicateUsername", func(t *testing.T) {
		res, err := svc.Register(ctx, "testuser", "other@example.com", "password123")
		if err != nil {
			t.Fatalf("Register failed: %v", err)
		}
		if res.Code != ErrCodeUserExists {
			t.Errorf("Expected UserExists code, got %d", res.Code)
		}
	})

	t.Run("InvalidInput", func(t *testing.T) {
		res, err := svc.Register(ctx, "", "", "")
		if err != nil {
			t.Fatalf("Register failed: %v", err)
		}
		if res.Code != ErrCodeInvalidInput {
			t.Errorf("Expected InvalidInput code, got %d", res.Code)
		}
	})
}

func TestAuthService_Login(t *testing.T) {
	userRepo := &repository.MockUserRepo{Users: make(map[string]*repository.User)}
	zoneRepo := &repository.MockZoneRepo{Zones: []*repository.Zone{{ID: 1, Name: "Zone 1", Status: "Online"}}}
	jwt := jwtutil.New("test_secret", 1)
	svc := New(userRepo, zoneRepo, jwt, nil)

	ctx := context.Background()

	svc.Register(ctx, "loginuser", "login@example.com", "password123")

	t.Run("Success", func(t *testing.T) {
		res, err := svc.Login(ctx, "loginuser", "password123")
		if err != nil {
			t.Fatalf("Login failed: %v", err)
		}
		if res.Code != ErrCodeSuccess {
			t.Errorf("Expected success code, got %d: %s", res.Code, res.Message)
		}
		if res.Token == "" {
			t.Error("Expected token, got empty")
		}
		if len(res.Zones) != 1 {
			t.Errorf("Expected 1 zone, got %d", len(res.Zones))
		}
	})

	t.Run("InvalidPassword", func(t *testing.T) {
		res, err := svc.Login(ctx, "loginuser", "wrongpassword")
		if err != nil {
			t.Fatalf("Login failed: %v", err)
		}
		if res.Code != ErrCodeInvalidCredential {
			t.Errorf("Expected InvalidCredential code, got %d", res.Code)
		}
	})

	t.Run("UserNotFound", func(t *testing.T) {
		res, err := svc.Login(ctx, "nonexistent", "password123")
		if err != nil {
			t.Fatalf("Login failed: %v", err)
		}
		if res.Code != ErrCodeInvalidCredential {
			t.Errorf("Expected InvalidCredential code, got %d", res.Code)
		}
	})
}
