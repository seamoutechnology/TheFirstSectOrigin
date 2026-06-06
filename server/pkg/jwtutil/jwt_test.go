package jwtutil

import (
	"testing"
	"time"
)

func TestJWTManager(t *testing.T) {
	secret := "test_secret"
	expireHours := 1
	manager := New(secret, expireHours)

	userID := int64(123)
	email := "test@example.com"

	t.Run("GenerateAndVerify", func(t *testing.T) {
		token, err := manager.Generate(userID, email)
		if err != nil {
			t.Fatalf("Failed to generate token: %v", err)
		}

		claims, err := manager.Verify(token)
		if err != nil {
			t.Fatalf("Failed to verify token: %v", err)
		}

		if claims.UserID != userID {
			t.Errorf("Expected userID %d, got %d", userID, claims.UserID)
		}
		if claims.Email != email {
			t.Errorf("Expected email %s, got %s", email, claims.Email)
		}
	})

	t.Run("InvalidToken", func(t *testing.T) {
		_, err := manager.Verify("invalid_token")
		if err == nil {
			t.Error("Expected error for invalid token, got nil")
		}
	})

	t.Run("ExpiredToken", func(t *testing.T) {
		shortManager := New(secret, -1) // Already expired
		token, err := shortManager.Generate(userID, email)
		if err != nil {
			t.Fatalf("Failed to generate token: %v", err)
		}

		time.Sleep(10 * time.Millisecond)

		_, err = manager.Verify(token)
		if err == nil {
			t.Error("Expected error for expired token, got nil")
		}
	})
}
