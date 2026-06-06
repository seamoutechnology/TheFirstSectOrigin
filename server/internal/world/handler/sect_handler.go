package handler

import (
	"context"

	"go.uber.org/zap"
	pb "server/pkg/pb"
)

func (h *WorldHandler) GetSectInfo(ctx context.Context, req *pb.GetProfileRequest) (*pb.SectInfo, error) {
	return &pb.SectInfo{
		Base: &pb.BaseResponse{Code: 0, Message: "msg_success"},
		SectName: "Thanh VAn MAn",
		Alignment: &pb.AlignmentInfo{
			CurrentAlignment: 1,
			KarmaPoints:      100,
			Title:            "Danh MAn ChAnh PhAi",
		},
		Buildings: []*pb.Building{
			{BuildingCode: "main_hall", Name: "Di Din", Level: 1},
		},
	}, nil
}

func (h *WorldHandler) UpgradeBuilding(ctx context.Context, req *pb.UpgradeBuildingRequest) (*pb.UpgradeBuildingResponse, error) {
	return &pb.UpgradeBuildingResponse{
		Base: &pb.BaseResponse{Code: 0, Message: "msg_upgrade_building_success"},
		Building: &pb.Building{BuildingCode: req.BuildingCode, Name: "New Building", Level: 2},
	}, nil
}

func (h *WorldHandler) CollectResources(ctx context.Context, req *pb.CollectResourcesRequest) (*pb.CollectResourcesResponse, error) {
	return &pb.CollectResourcesResponse{
		Base: &pb.BaseResponse{Code: 0, Message: "msg_collect_resources_success"},
		GoldGained: 100,
		Player: &pb.PlayerProfile{UserId: 1, Gold: 5100},
		Resources: map[string]int64{"gold": 100},
	}, nil
}

func (h *WorldHandler) SaveAdminMap(ctx context.Context, req *pb.SaveAdminMapRequest) (*pb.SaveAdminMapResponse, error) {
	err := h.svc.SaveAdminMap(ctx, "default_base", req.MapJsonData)
	if err != nil {
		h.log.Error("Failed to save admin map", zap.Error(err))
		return &pb.SaveAdminMapResponse{
			Base: &pb.BaseResponse{Code: 500, Message: "Failed to save admin map"},
		}, nil
	}

	return &pb.SaveAdminMapResponse{
		Base: &pb.BaseResponse{Code: 0, Message: "msg_success"},
	}, nil
}

func (h *WorldHandler) GetPlayerMap(ctx context.Context, req *pb.GetPlayerMapRequest) (*pb.GetPlayerMapResponse, error) {
	playerID, ok := h.getUserID(ctx)
	if !ok {
		return &pb.GetPlayerMapResponse{Base: errBase("msg_unauthorized")}, nil
	}

	jsonData, err := h.svc.GetPlayerMapWithFallback(ctx, playerID)
	
	if err != nil {
		h.log.Error("Error getting player map", zap.Error(err))
		return &pb.GetPlayerMapResponse{
			Base: &pb.BaseResponse{Code: 500, Message: "Database error"},
		}, nil
	}

	return &pb.GetPlayerMapResponse{
		Base: &pb.BaseResponse{Code: 0, Message: "msg_success"},
		MapJsonData: jsonData,
	}, nil
}

