package handler

import (
	"context"

	"go.uber.org/zap"
	pb "server/pkg/pb"
)

func (h *WorldHandler) DoGacha(ctx context.Context, req *pb.DoGachaRequest) (*pb.DoGachaResponse, error) {
	h.log.Info("DoGacha request", zap.Int32("banner", req.BannerId), zap.Int32("count", req.Count))
	
	return &pb.DoGachaResponse{
		Base: &pb.BaseResponse{Code: 0, Message: "msg_gacha_success"},
		Heroes: []*pb.Hero{
			{Id: 202, Name: "Trng Tiu PhAm", Rarity: "SR", Level: 1},
		},
	}, nil
}
