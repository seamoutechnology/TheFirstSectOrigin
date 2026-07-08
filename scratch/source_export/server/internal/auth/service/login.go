package service

import (
	"context"
	"errors"
	"fmt"
	"time"

	"golang.org/x/crypto/bcrypt"
	"server/internal/auth/repository"
)

type LoginResult struct {
	Code    int32
	Message string
	Token   string
	UserID  int64
	Zones   []*repository.Zone
}

func (s *AuthService) Login(ctx context.Context, username, password string) (*LoginResult, error) {
	user, err := s.userRepo.FindByUsername(ctx, username)
	if err != nil {
		if errors.Is(err, repository.ErrNotFound) {
			return &LoginResult{Code: ErrCodeInvalidCredential, Message: "Tên đăng nhập hoặc mật khẩu không đúng"}, nil
		}
		return nil, fmt.Errorf("find user: %w", err)
	}

	if user.IsBanned {
		msg := "Tài khoản bị khóa"
		if user.BanReason != nil && *user.BanReason != "" {
			msg = fmt.Sprintf("Tài khoản bị khóa: %s", *user.BanReason)
		}
		return &LoginResult{Code: ErrCodeUserBanned, Message: msg}, nil
	}

	fmt.Printf("[DEBUG] Login attempt for user: %s\n", username)
	fmt.Printf("[DEBUG] Password received length: %d\n", len(password))
	
	if err = bcrypt.CompareHashAndPassword([]byte(user.PasswordHash), []byte(password)); err != nil {
		fmt.Printf("[DEBUG] Bcrypt check FAILED: %v\n", err)
		return &LoginResult{Code: ErrCodeInvalidCredential, Message: "Tên đăng nhập hoặc mật khẩu không đúng"}, nil
	}
	fmt.Printf("[DEBUG] Bcrypt check SUCCESS\n")

	token, err := s.jwt.Generate(user.ID, user.Email)
	if err != nil {
		return nil, fmt.Errorf("generate token: %w", err)
	}

	expiration := time.Hour * 72 // Khớp với thời hạn JWT
	if err := s.SetActiveSession(ctx, user.ID, token, expiration); err != nil {
		return nil, fmt.Errorf("set active session: %w", err)
	}

	zones, err := s.zoneRepo.FindActive(ctx)
	if err != nil {
		return nil, fmt.Errorf("find zones: %w", err)
	}

	return &LoginResult{
		Code:    ErrCodeSuccess,
		Message: "Đăng nhập thành công",
		Token:   token,
		UserID:  user.ID,
		Zones:   zones,
	}, nil
}
