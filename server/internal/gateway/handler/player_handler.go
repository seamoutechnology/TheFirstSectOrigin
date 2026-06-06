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
