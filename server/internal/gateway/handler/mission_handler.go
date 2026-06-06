package handler

import (
	"context"
	pb "server/pkg/pb"
)

func (h *GatewayHandler) GetMissions(ctx context.Context, req *pb.GetMissionsRequest) (*pb.GetMissionsResponse, error) {
	return h.world.GetMissions(ctx, req)
}
