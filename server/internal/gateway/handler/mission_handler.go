package handler

import (
	"context"
	pb "server/pkg/pb"
)

func (h *GatewayHandler) GetMissions(ctx context.Context, req *pb.GetMissionsRequest) (*pb.GetMissionsResponse, error) {
	return h.world.GetMissions(ctx, req)
}

func (h *GatewayHandler) ClaimMissionReward(ctx context.Context, req *pb.ClaimMissionRewardRequest) (*pb.ClaimMissionRewardResponse, error) {
	return h.world.ClaimMissionReward(ctx, req)
}


