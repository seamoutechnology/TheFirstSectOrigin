package handler

import (
	"context"

	"go.uber.org/zap"
	pb "server/pkg/pb"
)

func (h *WorldHandler) GetPlayerProfile(ctx context.Context, req *pb.GetPlayerProfileRequest) (*pb.GetPlayerProfileResponse, error) {
	h.log.Info("GetPlayerProfile request")
	
	userID, ok := h.getUserID(ctx)
	if !ok {
		return &pb.GetPlayerProfileResponse{
			Base: &pb.BaseResponse{Code: 401, Message: "msg_unauthorized"},
		}, nil
	}

	player, code, msg := h.svc.GetPlayerProfile(ctx, userID)
	if code != 0 {
		return &pb.GetPlayerProfileResponse{
			Base: &pb.BaseResponse{Code: code, Message: msg},
		}, nil
	}
	
	return &pb.GetPlayerProfileResponse{
		Base: &pb.BaseResponse{Code: 0, Message: "msg_success"},
		Profile: &pb.PlayerProfile{
			UserId:     player.UserID,
			Nickname:   player.Nickname,
			Level:      int32(player.Level),
			Gold:       player.Gold,
			Diamond:    player.Diamond,
		},
	}, nil
}

func (h *WorldHandler) CreatePlayer(ctx context.Context, req *pb.CreatePlayerRequest) (*pb.CreatePlayerResponse, error) {
	h.log.Info("CreatePlayer request", zap.String("nickname", req.Nickname))
	
	userID, ok := h.getUserID(ctx)
	if !ok {
		return &pb.CreatePlayerResponse{
			Base: &pb.BaseResponse{Code: 401, Message: "msg_unauthorized"},
		}, nil
	}

	player, code, msg := h.svc.CreatePlayer(ctx, userID, req.Nickname)
	if code != 0 {
		return &pb.CreatePlayerResponse{
			Base: &pb.BaseResponse{Code: code, Message: msg},
		}, nil
	}
	
	return &pb.CreatePlayerResponse{
		Base: &pb.BaseResponse{Code: 0, Message: "msg_create_player_success"},
		Profile: &pb.PlayerProfile{
			UserId:     player.UserID,
			Nickname:   player.Nickname,
			Level:      int32(player.Level),
			Gold:       player.Gold,
			Diamond:    player.Diamond,
		},
	}, nil
}
