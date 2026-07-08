package handler

import (
	"context"

	"go.uber.org/zap"
	"server/internal/gateway/middleware"
	"server/internal/gateway/proxy"
	pb "server/pkg/pb"
)

type GatewayHandler struct {
	pb.UnimplementedGatewayServiceServer
	world  proxy.IWorldClient
	combat proxy.ICombatClient
	log    *zap.Logger
}

func New(world proxy.IWorldClient, combat proxy.ICombatClient, log *zap.Logger) *GatewayHandler {
	return &GatewayHandler{world: world, combat: combat, log: log}
}

func (h *GatewayHandler) getUserID(ctx context.Context) (int64, bool) {
	return middleware.GetUserID(ctx)
}

func errBase(msgId string) *pb.BaseResponse {
	return &pb.BaseResponse{Code: 401, Message: msgId}
}

var _ pb.GatewayServiceServer = (*GatewayHandler)(nil)
