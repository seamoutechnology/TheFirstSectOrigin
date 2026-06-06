package handler

import (
	"context"
	pb "server/pkg/pb"
)

func (h *GatewayHandler) DoGacha(ctx context.Context, req *pb.DoGachaRequest) (*pb.DoGachaResponse, error) {
	return h.world.DoGacha(ctx, req)
}
