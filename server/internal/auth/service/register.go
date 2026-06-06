package service

import (
	"context"
	"errors"
	"fmt"

	"golang.org/x/crypto/bcrypt"
	"server/internal/auth/repository"
)

const (
	ErrCodeSuccess           = 0
	ErrCodeInvalidInput      = 1001
	ErrCodeUserExists        = 1002
	ErrCodeInvalidCredential = 1003
	ErrCodeUserBanned        = 1004
	ErrCodeInternal          = 5000
)

type RegisterResult struct {
	Code    int32
	Message string
}

func (s *AuthService) Register(ctx context.Context, username, email, password string) (*RegisterResult, error) {
	if username == "" || email == "" || password == "" {
		return &RegisterResult{Code: ErrCodeInvalidInput, Message: "username, email và password không được để trống"}, nil
	}
	if len(password) < 6 {
		return &RegisterResult{Code: ErrCodeInvalidInput, Message: "password phải có ít nhất 6 ký tự"}, nil
	}

	if _, err := s.userRepo.FindByUsername(ctx, username); !errors.Is(err, repository.ErrNotFound) {
		return &RegisterResult{Code: ErrCodeUserExists, Message: "username đã được sử dụng"}, nil
	}

	if _, err := s.userRepo.FindByEmail(ctx, email); !errors.Is(err, repository.ErrNotFound) {
		return &RegisterResult{Code: ErrCodeUserExists, Message: "email đã được sử dụng"}, nil
	}

	hashBytes, err := bcrypt.GenerateFromPassword([]byte(password), bcrypt.DefaultCost)
	if err != nil {
		return nil, fmt.Errorf("hash password: %w", err)
	}

	if _, err = s.userRepo.Create(ctx, username, email, string(hashBytes)); err != nil {
		return nil, fmt.Errorf("create user: %w", err)
	}

	return &RegisterResult{Code: ErrCodeSuccess, Message: "Đăng ký thành công"}, nil
}
