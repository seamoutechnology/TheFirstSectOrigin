package handler

import (
	"context"
	pb "server/pkg/pb"
)

func (h *GatewayHandler) CreatePlayer(ctx context.Context, req *pb.CreatePlayerRequest) (*pb.CreatePlayerResponse, error) {
	return h.world.CreatePlayer(ctx, req)
}

func (h *GatewayHandler) GetPlayerProfile(ctx context.Context, req *pb.GetPlayerProfileRequest) (*pb.GetPlayerProfileResponse, error) {
	return h.world.GetPlayerProfile(ctx, req)
}

func (h *GatewayHandler) GetCompletedStages(ctx context.Context, req *pb.GetCompletedStagesRequest) (*pb.GetCompletedStagesResponse, error) {
	return h.world.GetCompletedStages(ctx, req)
}

func (h *GatewayHandler) GetLeaderboard(ctx context.Context, req *pb.GetLeaderboardRequest) (*pb.GetLeaderboardResponse, error) {
	return h.world.GetLeaderboard(ctx, req)
}
