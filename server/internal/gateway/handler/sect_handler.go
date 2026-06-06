package handler

import (
	"context"
	pb "server/pkg/pb"
)

func (h *GatewayHandler) GetSectInfo(ctx context.Context, req *pb.GetProfileRequest) (*pb.SectInfo, error) {
	return h.world.GetSectInfo(ctx, req)
}

func (h *GatewayHandler) UpgradeBuilding(ctx context.Context, req *pb.UpgradeBuildingRequest) (*pb.UpgradeBuildingResponse, error) {
	return h.world.UpgradeBuilding(ctx, req)
}

func (h *GatewayHandler) CollectResources(ctx context.Context, req *pb.CollectResourcesRequest) (*pb.CollectResourcesResponse, error) {
	return h.world.CollectResources(ctx, req)
}

func (h *GatewayHandler) SaveAdminMap(ctx context.Context, req *pb.SaveAdminMapRequest) (*pb.SaveAdminMapResponse, error) {
	return h.world.SaveAdminMap(ctx, req)
}

func (h *GatewayHandler) GetPlayerMap(ctx context.Context, req *pb.GetPlayerMapRequest) (*pb.GetPlayerMapResponse, error) {
	return h.world.GetPlayerMap(ctx, req)
}
