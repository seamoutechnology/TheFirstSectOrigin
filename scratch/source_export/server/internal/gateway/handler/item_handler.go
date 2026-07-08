package handler

import (
	"context"
	pb "server/pkg/pb"
)

func (h *GatewayHandler) GetInventory(ctx context.Context, req *pb.GetProfileRequest) (*pb.Inventory, error) {
	return h.world.GetInventory(ctx, req)
}

func (h *GatewayHandler) EquipItem(ctx context.Context, req *pb.EquipRequest) (*pb.EquipResponse, error) {
	return h.world.EquipItem(ctx, req)
}

func (h *GatewayHandler) UseItem(ctx context.Context, req *pb.UseItemRequest) (*pb.UseItemResponse, error) {
	return h.world.UseItem(ctx, req)
}

func (h *GatewayHandler) BuyShopItem(ctx context.Context, req *pb.BuyShopItemRequest) (*pb.BuyShopItemResponse, error) {
	return h.world.BuyShopItem(ctx, req)
}

func (h *GatewayHandler) GetShop(ctx context.Context, req *pb.GetShopRequest) (*pb.GetShopResponse, error) {
	return h.world.GetShop(ctx, req)
}

func (h *GatewayHandler) RefreshShop(ctx context.Context, req *pb.RefreshShopRequest) (*pb.RefreshShopResponse, error) {
	return h.world.RefreshShop(ctx, req)
}



