package handler

import (
	"context"

	"go.uber.org/zap"
	"server/internal/world/repository"
	pb "server/pkg/pb"
)

func toPbBuilding(b *repository.PlayerBuilding) *pb.Building {
	if b == nil {
		return nil
	}
	var upgradeEndAt int64
	if b.UpgradeEndAt != nil {
		upgradeEndAt = b.UpgradeEndAt.Unix()
	}
	return &pb.Building{
		BuildingCode:  b.BuildingCode,
		Name:          b.BuildingName,
		Level:         b.Level,
		UpgradeEndAt:  upgradeEndAt,
		LastCollectAt: b.LastCollectAt.Unix(),
		MaxLevel:      b.MaxLevel,
		InstanceId:    b.ID,
	}
}

func (h *WorldHandler) GetBase(ctx context.Context, req *pb.GetBaseRequest) (*pb.GetBaseResponse, error) {
	userID, ok := h.getUserID(ctx)
	if !ok {
		return &pb.GetBaseResponse{Base: errBase("msg_unauthorized")}, nil
	}

	buildings, code, msg := h.svc.GetBase(ctx, userID)
	if code != 0 {
		return &pb.GetBaseResponse{
			Base: &pb.BaseResponse{Code: code, Message: msg},
		}, nil
	}

	pbBuildings := make([]*pb.Building, 0, len(buildings))
	for _, b := range buildings {
		pbBuildings = append(pbBuildings, toPbBuilding(b))
	}

	return &pb.GetBaseResponse{
		Base:      &pb.BaseResponse{Code: 0, Message: "msg_success"},
		Buildings: pbBuildings,
	}, nil
}

func (h *WorldHandler) GetSectInfo(ctx context.Context, req *pb.GetProfileRequest) (*pb.SectInfo, error) {
	return &pb.SectInfo{
		Base: &pb.BaseResponse{Code: 0, Message: "msg_success"},
		SectName: "Thanh Vân Môn",
		Alignment: &pb.AlignmentInfo{
			CurrentAlignment: 1,
			KarmaPoints:      100,
			Title:            "Danh Môn Chính Phái",
		},
		Buildings: []*pb.Building{
			{BuildingCode: "main_hall", Name: "Đại Điện", Level: 1},
		},
	}, nil
}

func (h *WorldHandler) UpgradeBuilding(ctx context.Context, req *pb.UpgradeBuildingRequest) (*pb.UpgradeBuildingResponse, error) {
	userID, ok := h.getUserID(ctx)
	if !ok {
		return &pb.UpgradeBuildingResponse{Base: errBase("msg_unauthorized")}, nil
	}

	building, _, code, msg := h.svc.UpgradeBuilding(ctx, userID, req.InstanceId)
	if code != 0 {
		return &pb.UpgradeBuildingResponse{
			Base: &pb.BaseResponse{Code: code, Message: msg},
		}, nil
	}

	return &pb.UpgradeBuildingResponse{
		Base:     &pb.BaseResponse{Code: 0, Message: "msg_upgrade_building_success"},
		Building: toPbBuilding(building),
	}, nil
}

func (h *WorldHandler) SpeedUpBuilding(ctx context.Context, req *pb.SpeedUpBuildingRequest) (*pb.SpeedUpBuildingResponse, error) {
	userID, ok := h.getUserID(ctx)
	if !ok {
		return &pb.SpeedUpBuildingResponse{Base: errBase("msg_unauthorized")}, nil
	}

	building, player, code, msg := h.svc.SpeedUpBuilding(ctx, userID, req.InstanceId)
	if code != 0 {
		return &pb.SpeedUpBuildingResponse{
			Base: &pb.BaseResponse{Code: code, Message: msg},
		}, nil
	}

	var pbPlayer *pb.PlayerProfile
	if player != nil {
		pbPlayer = &pb.PlayerProfile{
			UserId:     player.UserID,
			Nickname:   player.Nickname,
			Level:      player.Level,
			Exp:        player.Exp,
			Gold:       player.Gold,
			Diamond:    player.Diamond,
			Stamina:    player.Stamina,
			MaxStamina: player.MaxStamina,
		}
	}

	return &pb.SpeedUpBuildingResponse{
		Base:     &pb.BaseResponse{Code: 0, Message: "msg_speed_up_building_success"},
		Building: toPbBuilding(building),
		Player:   pbPlayer,
	}, nil
}

func (h *WorldHandler) CollectResources(ctx context.Context, req *pb.CollectResourcesRequest) (*pb.CollectResourcesResponse, error) {
	userID, ok := h.getUserID(ctx)
	if !ok {
		return &pb.CollectResourcesResponse{Base: errBase("msg_unauthorized")}, nil
	}

	goldGained, player, code, msg := h.svc.CollectResources(ctx, userID, req.InstanceId)
	if code != 0 {
		return &pb.CollectResourcesResponse{
			Base: &pb.BaseResponse{Code: code, Message: msg},
		}, nil
	}

	var pbPlayer *pb.PlayerProfile
	if player != nil {
		pbPlayer = &pb.PlayerProfile{
			UserId:     player.UserID,
			Nickname:   player.Nickname,
			Level:      player.Level,
			Exp:        player.Exp,
			Gold:       player.Gold,
			Diamond:    player.Diamond,
			Stamina:    player.Stamina,
			MaxStamina: player.MaxStamina,
		}
	}

	return &pb.CollectResourcesResponse{
		Base: &pb.BaseResponse{Code: 0, Message: "msg_collect_resources_success"},
		GoldGained: goldGained,
		Player: pbPlayer,
		Resources: map[string]int64{"gold": goldGained},
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

func (h *WorldHandler) SavePlayerMap(ctx context.Context, req *pb.SavePlayerMapRequest) (*pb.SavePlayerMapResponse, error) {
	playerID, ok := h.getUserID(ctx)
	if !ok {
		return &pb.SavePlayerMapResponse{Base: errBase("msg_unauthorized")}, nil
	}

	err := h.svc.SavePlayerMap(ctx, playerID, req.MapJsonData)
	if err != nil {
		h.log.Error("Error saving player map", zap.Error(err))
		return &pb.SavePlayerMapResponse{
			Base: &pb.BaseResponse{Code: 500, Message: err.Error()},
		}, nil
	}

	return &pb.SavePlayerMapResponse{
		Base: &pb.BaseResponse{Code: 0, Message: "msg_success"},
	}, nil
}

