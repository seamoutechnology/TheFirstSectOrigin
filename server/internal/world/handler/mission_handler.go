package handler

import (
	"context"

	"go.uber.org/zap"
	pb "server/pkg/pb"
)

func (h *WorldHandler) GetMissions(ctx context.Context, req *pb.GetMissionsRequest) (*pb.GetMissionsResponse, error) {
	h.log.Info("GetMissions request")

	return &pb.GetMissionsResponse{
		Code:      0,
		MessageId: "msg_success",
		Missions: []*pb.Mission{
			{
				MissionId:       1,
				Title:           "Sơ nhập tông môn",
				Description:     "Nói chuyện với Chưởng môn",
				Type:            pb.MissionType_MAIN,
				Status:          pb.MissionStatus_AVAILABLE,
				CurrentProgress: 0,
				TargetProgress:  1,
				Rewards:         map[string]int32{"gold": 100, "exp": 50},
			},
		},
	}, nil
}

func (h *WorldHandler) CompleteMission(ctx context.Context, req *pb.CompleteMissionRequest) (*pb.CompleteMissionResponse, error) {
	h.log.Info("CompleteMission request", zap.Int32("id", req.MissionId))

	return &pb.CompleteMissionResponse{
		Code:      0,
		MessageId: "msg_mission_completed",
		Rewards: map[string]int32{"gold": 100, "exp": 50},
	}, nil
}
