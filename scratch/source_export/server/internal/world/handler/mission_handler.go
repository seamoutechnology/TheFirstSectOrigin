package handler

import (
	"context"

	"go.uber.org/zap"
	pb "server/pkg/pb"
)

func (h *WorldHandler) GetMissions(ctx context.Context, req *pb.GetMissionsRequest) (*pb.GetMissionsResponse, error) {
	h.log.Info("GetMissions request", zap.String("type", req.FilterType.String()))
	userID, ok := h.getUserID(ctx)
	if !ok {
		return &pb.GetMissionsResponse{Code: 401, MessageId: "msg_err_unauthorized"}, nil
	}

	missions, code, msg := h.svc.GetMissions(ctx, userID, req.FilterType)
	return &pb.GetMissionsResponse{
		Code:      code,
		MessageId: msg,
		Missions:  missions,
	}, nil
}

func (h *WorldHandler) CompleteMission(ctx context.Context, req *pb.CompleteMissionRequest) (*pb.CompleteMissionResponse, error) {
	h.log.Info("CompleteMission request", zap.Int32("id", req.MissionId))
	
	// fallback matching ClaimMissionReward logic
	userID, ok := h.getUserID(ctx)
	if !ok {
		return &pb.CompleteMissionResponse{Code: 401, MessageId: "msg_err_unauthorized"}, nil
	}

	claimReq := &pb.ClaimMissionRewardRequest{MissionId: req.MissionId}
	code, msg := h.svc.ClaimMissionReward(ctx, userID, claimReq)
	
	return &pb.CompleteMissionResponse{
		Code:      code,
		MessageId: msg,
	}, nil
}

func (h *WorldHandler) ClaimMissionReward(ctx context.Context, req *pb.ClaimMissionRewardRequest) (*pb.ClaimMissionRewardResponse, error) {
	h.log.Info("ClaimMissionReward request", zap.Int32("id", req.MissionId))
	userID, ok := h.getUserID(ctx)
	if !ok {
		return &pb.ClaimMissionRewardResponse{Code: 401, MessageId: "msg_err_unauthorized"}, nil
	}

	code, msg := h.svc.ClaimMissionReward(ctx, userID, req)
	return &pb.ClaimMissionRewardResponse{
		Code:      code,
		MessageId: msg,
	}, nil
}

