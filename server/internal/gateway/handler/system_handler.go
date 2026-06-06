package handler

import (
	"context"
	pb "server/pkg/pb"
)

func (h *GatewayHandler) GetVersion(ctx context.Context, req *pb.GetVersionRequest) (*pb.GetVersionResponse, error) {
	return h.world.GetVersion(ctx, req)
}
