package service

import (
	"context"
	"fmt"
	"time"

	"golang.org/x/crypto/bcrypt"
)

func (s *AuthService) Logout(ctx context.Context, token string) error {
	claims, err := s.jwt.Verify(token)
	if err != nil {
		return err
	}
	
	return s.redis.Del(ctx, s.sessionKey(claims.UserID)).Err()
}

func (s *AuthService) ForgetPassword(ctx context.Context, email string) error {
	user, err := s.userRepo.FindByEmail(ctx, email)
	if err != nil {
		return err
	}
	fmt.Printf("Yêu cầu khôi phục mật khẩu cho user: %s\n", user.Username)

	otp := "123456" // Demo, thực tế dùng rand.Intn
	
	otpKey := fmt.Sprintf("otp:fp:%s", email)
	s.redis.Set(ctx, otpKey, otp, 5*time.Minute)

	fmt.Printf("Gửi email tới %s: Mã khôi phục mật khẩu của bạn là %s\n", email, otp)
	
	return nil
}

func (s *AuthService) ResetPassword(ctx context.Context, email, otp, newPassword string) error {
	otpKey := fmt.Sprintf("otp:fp:%s", email)
	savedOtp, err := s.redis.Get(ctx, otpKey).Result()
	if err != nil {
		return fmt.Errorf("OTP expired or invalid")
	}

	if savedOtp != otp {
		return fmt.Errorf("wrong OTP")
	}

	hash, _ := bcrypt.GenerateFromPassword([]byte(newPassword), bcrypt.DefaultCost)
	
	// TODO: Cập nhật vào DB qua repository
	fmt.Printf("Cập nhật mật khẩu cho %s\n", email)
	_ = hash 

	s.redis.Del(ctx, otpKey)
	
	return nil
}

func (s *AuthService) Enable2FA(ctx context.Context, userID int64) (string, string, error) {
	secret := "SECRET_KEY_DEMO"
	qrCode := "https://api.qrserver.com/v1/create-qr-code/?data=OTP_AUTH_DEMO"
	
	// TODO: Lưu secret vào DB của user
	
	return secret, qrCode, nil
}

func (s *AuthService) Verify2FA(ctx context.Context, userID int64, code string) error {
	if code == "123456" { // Demo
		return nil
	}
	return fmt.Errorf("invalid 2FA code")
}
