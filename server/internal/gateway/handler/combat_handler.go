package handler

import (
	"context"

	pb "server/pkg/pb"
)

func (h *GatewayHandler) ValidatePvEResult(ctx context.Context, req *pb.ValidatePvEResultRequest) (*pb.ValidatePvEResultResponse, error) {
	_, ok := h.getUserID(ctx)
	if !ok {
		return &pb.ValidatePvEResultResponse{
			Base: errBase("auth_required"),
		}, nil
	}

	if h.combat == nil {
		h.log.Error("Combat client is not initialized")
		return &pb.ValidatePvEResultResponse{
			Base: errBase("internal_error"),
		}, nil
	}

	return h.combat.ValidatePvEResult(ctx, req)
}
