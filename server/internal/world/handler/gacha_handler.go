package handler

import (
	"context"
	"go.uber.org/zap"
	pb "server/pkg/pb"
)

func (h *WorldHandler) GetGachaBanners(ctx context.Context, req *pb.GetGachaBannersRequest) (*pb.GetGachaBannersResponse, error) {
	h.log.Info("GetGachaBanners request")
	banners, code, msg := h.svc.GetGachaBanners(ctx)
	if code != 0 {
		return &pb.GetGachaBannersResponse{
			Base: &pb.BaseResponse{Code: code, Message: msg},
		}, nil
	}

	pbBanners := make([]*pb.GachaBanner, 0, len(banners))
	for _, b := range banners {
		pbBanners = append(pbBanners, &pb.GachaBanner{
			BannerId:    b.ID,
			Name:        b.Name,
			Description: b.Description,
			CostDiamond: b.CostDiamond,
			EndTime:     b.EndAt.Format("2006-01-02 15:04:05"),
			CostItem:    b.CostItem,
			CostGold:    b.CostGold,
		})
	}

	return &pb.GetGachaBannersResponse{
		Base:    &pb.BaseResponse{Code: 0, Message: "msg_success"},
		Banners: pbBanners,
	}, nil
}

func (h *WorldHandler) DoGacha(ctx context.Context, req *pb.DoGachaRequest) (*pb.DoGachaResponse, error) {
	h.log.Info("DoGacha request", zap.Int32("banner", req.BannerId), zap.Int32("count", req.Count))

	userID, ok := h.getUserID(ctx)
	if !ok {
		return &pb.DoGachaResponse{Base: &pb.BaseResponse{Code: 401, Message: "msg_unauthorized"}}, nil
	}

	results, updatedPlayer, code, msg := h.svc.DoGacha(ctx, userID, req.BannerId, req.Count)
	if code != 0 {
		return &pb.DoGachaResponse{
			Base: &pb.BaseResponse{Code: code, Message: msg},
		}, nil
	}

	pbHeroes := make([]*pb.Hero, 0, len(results))
	for _, hero := range results {
		pbHeroes = append(pbHeroes, &pb.Hero{
			Id:     hero.ID,
			Name:   hero.Name,
			Level:  hero.Level,
			Rarity: hero.Rarity,
			Power:  int64(hero.BaseATK*10 + hero.BaseHP + hero.BaseDEF*5), // Calculate Power
			Star:   hero.Star,
		})
	}

	return &pb.DoGachaResponse{
		Base: &pb.BaseResponse{Code: 0, Message: "msg_gacha_success"},
		PlayerAfter: &pb.PlayerProfile{
			UserId:     updatedPlayer.UserID,
			Nickname:   updatedPlayer.Nickname,
			Level:      updatedPlayer.Level,
			Exp:        updatedPlayer.Exp,
			Gold:       updatedPlayer.Gold,
			Diamond:    updatedPlayer.Diamond,
			Stamina:    updatedPlayer.Stamina,
			MaxStamina: updatedPlayer.MaxStamina,
		},
		Heroes: pbHeroes,
	}, nil
}
