package handler

import (
	"context"

	"go.uber.org/zap"
	pb "server/pkg/pb"
)

func (h *WorldHandler) GetInventory(ctx context.Context, req *pb.GetProfileRequest) (*pb.Inventory, error) {
	h.log.Info("GetInventory request")
	// TODO: Thực hiện gọi service lấy dữ liệu từ DB
	return &pb.Inventory{
		Items: []*pb.Item{
			{Id: 1, ItemCode: "iron_sword", Name: "Kiếm Sắt", Type: "equipment", Rarity: "common", Quantity: 1},
		},
	}, nil
}

func (h *WorldHandler) EquipItem(ctx context.Context, req *pb.EquipRequest) (*pb.EquipResponse, error) {
	h.log.Info("EquipItem request", 
		zap.Int64("disciple_id", req.DiscipleId), 
		zap.Int64("item_id", req.ItemId),
		zap.String("slot", req.Slot))
		
	// TODO: Logic kiểm tra túi đồ và cập nhật bảng item_instances
	
	return &pb.EquipResponse{
		Code:      0,
		MessageId: "msg_equip_success",
	}, nil
}

func (h *WorldHandler) CraftItem(ctx context.Context, req *pb.CraftRequest) (*pb.CraftResponse, error) {
	h.log.Info("CraftItem request", zap.String("recipe_id", req.RecipeId))

	// TODO: Logic trừ nguyên liệu và tính toán tỉ lệ thành công
	
	return &pb.CraftResponse{
		Code:      0,
		MessageId: "msg_craft_success",
	}, nil
}
