package handler

import (
	"context"

	"go.uber.org/zap"
	"google.golang.org/grpc/codes"
	"google.golang.org/grpc/status"

	"server/internal/auth/service"
	pb "server/pkg/pb"
)

type AuthHandler struct {
	pb.UnimplementedAuthServiceServer
	svc *service.AuthService
	log *zap.Logger
}

func New(svc *service.AuthService, log *zap.Logger) *AuthHandler {
	return &AuthHandler{svc: svc, log: log}
}

func (h *AuthHandler) Register(ctx context.Context, req *pb.RegisterRequest) (*pb.RegisterResponse, error) {
	h.log.Info("Register request", zap.String("username", req.Username), zap.String("email", req.Email))

	result, err := h.svc.Register(ctx, req.Username, req.Email, req.Password)
	if err != nil {
		h.log.Error("Register internal error", zap.Error(err))
		return nil, status.Errorf(codes.Internal, "internal server error")
	}

	return &pb.RegisterResponse{
		Code:      result.Code,
		MessageId: "msg_register_success",
	}, nil
}

func (h *AuthHandler) Login(ctx context.Context, req *pb.LoginRequest) (*pb.LoginResponse, error) {
	h.log.Info("Login request", zap.String("username", req.Username))

	result, err := h.svc.Login(ctx, req.Username, req.Password)
	if err != nil {
		h.log.Error("Login internal error", zap.Error(err))
		return nil, status.Errorf(codes.Internal, "internal server error")
	}

	resp := &pb.LoginResponse{
		Code:      result.Code,
		MessageId: result.Message, // Trả về message thực tế (Sai pass, account ko tồn tại...)
		Token:     result.Token,
		UserId:    result.UserID,
	}

	if result.Code == service.ErrCodeSuccess {
		zoneList := &pb.ZoneList{}
		for _, z := range result.Zones {
			zoneList.Zones = append(zoneList.Zones, &pb.Zone{
				ZoneId:     int32(z.ID),
				Name:       z.Name,
				Status:     z.Status,
				GatewayUrl: z.GatewayURL,
			})
		}
		resp.ZoneList = zoneList
	}

	return resp, nil
}

func (h *AuthHandler) Logout(ctx context.Context, req *pb.LogoutRequest) (*pb.BaseResponse, error) {
	h.log.Info("Logout request")
	err := h.svc.Logout(ctx, req.Token)
	if err != nil {
		return &pb.BaseResponse{Code: 5000, Message: "msg_logout_failed"}, nil
	}
	return &pb.BaseResponse{Code: 0, Message: "msg_logout_success"}, nil
}

func (h *AuthHandler) ForgetPassword(ctx context.Context, req *pb.ForgetPasswordRequest) (*pb.BaseResponse, error) {
	h.log.Info("ForgetPassword request", zap.String("email", req.Email))
	err := h.svc.ForgetPassword(ctx, req.Email)
	if err != nil {
		return &pb.BaseResponse{Code: 5000, Message: "msg_forget_password_failed"}, nil
	}
	return &pb.BaseResponse{Code: 0, Message: "msg_forget_password_sent"}, nil
}

func (h *AuthHandler) ResetPassword(ctx context.Context, req *pb.ResetPasswordRequest) (*pb.BaseResponse, error) {
	h.log.Info("ResetPassword request", zap.String("email", req.Email))
	err := h.svc.ResetPassword(ctx, req.Email, req.Otp, req.NewPassword)
	if err != nil {
		return &pb.BaseResponse{Code: 5000, Message: "msg_reset_password_failed"}, nil
	}
	return &pb.BaseResponse{Code: 0, Message: "msg_reset_password_success"}, nil
}

func (h *AuthHandler) Enable2FA(ctx context.Context, req *pb.BaseRequest) (*pb.Enable2FAResponse, error) {
	secret, qrCode, err := h.svc.Enable2FA(ctx, 1) 
	if err != nil {
		return &pb.Enable2FAResponse{Code: 5000, MessageId: "msg_2fa_enable_failed"}, nil
	}
	return &pb.Enable2FAResponse{
		Code:      0,
		MessageId: "msg_success",
		Secret:  secret,
		QrCode:  qrCode,
	}, nil
}

func (h *AuthHandler) Verify2FA(ctx context.Context, req *pb.Verify2FARequest) (*pb.BaseResponse, error) {
	err := h.svc.Verify2FA(ctx, 1, req.Code)
	if err != nil {
		return &pb.BaseResponse{Code: 5000, Message: "msg_2fa_verify_failed"}, nil
	}
	return &pb.BaseResponse{Code: 0, Message: "msg_2fa_verified"}, nil
}

var _ pb.AuthServiceServer = (*AuthHandler)(nil)

