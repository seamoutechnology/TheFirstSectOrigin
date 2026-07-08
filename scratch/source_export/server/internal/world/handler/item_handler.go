package handler

import (
	"context"

	"go.uber.org/zap"
	pb "server/pkg/pb"
)

func (h *WorldHandler) GetInventory(ctx context.Context, req *pb.GetProfileRequest) (*pb.Inventory, error) {
	h.log.Info("GetInventory request")
	userID, ok := h.getUserID(ctx)
	if !ok {
		h.log.Warn("GetInventory: user-id not found in metadata context")
		return &pb.Inventory{}, nil
	}

	h.log.Info("GetInventory: fetching items for user", zap.Int64("user_id", userID))
	items, code, msg := h.svc.GetInventory(ctx, userID)
	if code != 0 {
		h.log.Error("Failed to get inventory", zap.String("msg", msg), zap.Int32("code", code))
		return &pb.Inventory{}, nil
	}

	h.log.Info("GetInventory: found items count", zap.Int("count", len(items)))

	pbItems := make([]*pb.Item, 0, len(items))
	for _, item := range items {
		pbItems = append(pbItems, &pb.Item{
			Id:       item.ID,
			ItemCode: item.ItemCode,
			Quantity: item.Quantity,
		})
	}

	allConfigs, err := h.svc.GetAllItemConfigs(ctx)
	pbConfigs := make([]*pb.ItemConfig, 0)
	if err == nil {
		for _, cfg := range allConfigs {
			pbConfigs = append(pbConfigs, &pb.ItemConfig{
				ItemCode:      cfg.ItemCode,
				NameKey:       cfg.NameKey,
				Type:          cfg.Type,
				Rarity:        cfg.Rarity,
				Icon:          cfg.Icon,
				DescKey:       cfg.DescKey,
				MaxStack:      cfg.MaxStack,
				RequiredLevel: cfg.RequiredLevel,
				Sources:       make([]*pb.ItemSource, 0),
				Effects:       make([]*pb.ItemEffect, 0),
			})
		}
	} else {
		h.log.Error("Failed to get all item configs", zap.Error(err))
	}

	return &pb.Inventory{
		Items:   pbItems,
		Configs: pbConfigs,
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

func (h *WorldHandler) UseItem(ctx context.Context, req *pb.UseItemRequest) (*pb.UseItemResponse, error) {
	h.log.Info("UseItem request", zap.Int64("item_id", req.ItemId), zap.Int32("quantity", req.Quantity))
	userID, ok := h.getUserID(ctx)
	if !ok {
		return &pb.UseItemResponse{Code: 401, MessageId: "msg_err_unauthorized"}, nil
	}

	code, msg := h.svc.UseItem(ctx, userID, req)
	return &pb.UseItemResponse{
		Code:      code,
		MessageId: msg,
	}, nil
}

func (h *WorldHandler) BuyShopItem(ctx context.Context, req *pb.BuyShopItemRequest) (*pb.BuyShopItemResponse, error) {
	h.log.Info("BuyShopItem request", zap.Int64("instance_id", req.InstanceId), zap.Int32("quantity", req.Quantity))
	userID, ok := h.getUserID(ctx)
	if !ok {
		return &pb.BuyShopItemResponse{Code: 401, MessageId: "msg_err_unauthorized"}, nil
	}

	items, code, msg := h.svc.BuyShopItem(ctx, userID, req)
	return &pb.BuyShopItemResponse{
		Code:        code,
		MessageId:   msg,
		GainedItems: items,
	}, nil
}

func (h *WorldHandler) GetShop(ctx context.Context, req *pb.GetShopRequest) (*pb.GetShopResponse, error) {
	h.log.Info("GetShop request", zap.String("shop_type", req.ShopType))
	userID, ok := h.getUserID(ctx)
	if !ok {
		return &pb.GetShopResponse{Code: 401, MessageId: "msg_err_unauthorized"}, nil
	}

	items, nextRefresh, code, msg := h.svc.GetShop(ctx, userID, req.ShopType)
	return &pb.GetShopResponse{
		Code:          code,
		MessageId:     msg,
		Items:         items,
		NextRefreshAt: nextRefresh,
	}, nil
}

func (h *WorldHandler) RefreshShop(ctx context.Context, req *pb.RefreshShopRequest) (*pb.RefreshShopResponse, error) {
	h.log.Info("RefreshShop request", zap.String("shop_type", req.ShopType))
	userID, ok := h.getUserID(ctx)
	if !ok {
		return &pb.RefreshShopResponse{Code: 401, MessageId: "msg_err_unauthorized"}, nil
	}

	items, nextRefresh, code, msg := h.svc.RefreshShop(ctx, userID, req.ShopType)
	return &pb.RefreshShopResponse{
		Code:          code,
		MessageId:     msg,
		Items:         items,
		NextRefreshAt: nextRefresh,
	}, nil
}



