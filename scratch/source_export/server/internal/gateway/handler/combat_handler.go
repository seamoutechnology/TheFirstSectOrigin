package handler

import (
	"context"

	pb "server/pkg/pb"
	"go.uber.org/zap"
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

	// 1. Validate result on combat server
	resp, err := h.combat.ValidatePvEResult(ctx, req)
	if err != nil {
		return nil, err
	}

	// 2. If validation succeeded (meaning it's valid victory or valid defeat),
	// forward the call to world server to deduct stamina and add rewards.
	if resp.IsValid {
		worldResp, worldErr := h.world.ValidatePvEResult(ctx, req)
		if worldErr != nil {
			h.log.Error("Failed to deduct stamina and reward player on world server", zap.Error(worldErr))
			return resp, nil
		}
		return worldResp, nil
	}

	return resp, nil
}

