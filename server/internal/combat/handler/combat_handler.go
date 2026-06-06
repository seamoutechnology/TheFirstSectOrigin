package handler

import (
	"context"

	"server/internal/combat/service"
	"server/pkg/pb"

	"go.uber.org/zap"
)

type CombatHandler struct {
	pb.UnimplementedCombatServiceServer
	svc *service.CombatService
	log *zap.Logger
}

func NewCombatHandler(svc *service.CombatService, log *zap.Logger) *CombatHandler {
	return &CombatHandler{
		svc: svc,
		log: log,
	}
}

func (h *CombatHandler) ValidatePvEResult(ctx context.Context, req *pb.ValidatePvEResultRequest) (*pb.ValidatePvEResultResponse, error) {
	return h.svc.ValidatePvEResult(ctx, req)
}
