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
			Stamina:    player.Stamina,
			MaxStamina: player.MaxStamina,
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
			Stamina:    player.Stamina,
			MaxStamina: player.MaxStamina,
		},
	}, nil
}

func (h *WorldHandler) ValidatePvEResult(ctx context.Context, req *pb.ValidatePvEResultRequest) (*pb.ValidatePvEResultResponse, error) {
	h.log.Info("WorldHandler ValidatePvEResult request", zap.String("enemy", req.EnemyId), zap.Bool("is_victory", req.IsVictory))

	userID, ok := h.getUserID(ctx)
	if !ok {
		return &pb.ValidatePvEResultResponse{
			Base: &pb.BaseResponse{Code: 401, Message: "msg_unauthorized"},
		}, nil
	}

	_, code, msg := h.svc.ProcessPvECombatResult(ctx, userID, req)
	if code != 0 {
		return &pb.ValidatePvEResultResponse{
			Base: &pb.BaseResponse{Code: code, Message: msg},
		}, nil
	}

	resp := &pb.ValidatePvEResultResponse{
		Base:    &pb.BaseResponse{Code: 0, Message: "msg_success"},
		IsValid: true,
	}

	if req.IsVictory {
		resp.RewardExp = 100
		resp.RewardLinhThach = 50
	}

	return resp, nil
}

func (h *WorldHandler) GetCompletedStages(ctx context.Context, req *pb.GetCompletedStagesRequest) (*pb.GetCompletedStagesResponse, error) {
	userID, ok := h.getUserID(ctx)
	if !ok {
		return &pb.GetCompletedStagesResponse{Base: &pb.BaseResponse{Code: 401, Message: "msg_unauthorized"}}, nil
	}
	stages, code, msg := h.svc.GetCompletedStages(ctx, userID)
	if code != 0 {
		return &pb.GetCompletedStagesResponse{Base: &pb.BaseResponse{Code: code, Message: msg}}, nil
	}
	return &pb.GetCompletedStagesResponse{
		Base:     &pb.BaseResponse{Code: 0, Message: "msg_success"},
		StageIds: stages,
	}, nil
}

func (h *WorldHandler) GetLeaderboard(ctx context.Context, req *pb.GetLeaderboardRequest) (*pb.GetLeaderboardResponse, error) {
	records, code, msg := h.svc.GetLeaderboard(ctx, req.Type)
	if code != 0 {
		return &pb.GetLeaderboardResponse{Base: &pb.BaseResponse{Code: code, Message: msg}}, nil
	}
	var pbEntries []*pb.LeaderboardEntry
	for _, r := range records {
		pbEntries = append(pbEntries, &pb.LeaderboardEntry{
			Rank:     r.Rank,
			Nickname: r.Nickname,
			Value:    r.Value,
		})
	}
	return &pb.GetLeaderboardResponse{
		Base:    &pb.BaseResponse{Code: 0, Message: "msg_success"},
		Entries: pbEntries,
	}, nil
}

