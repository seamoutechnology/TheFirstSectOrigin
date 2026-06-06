package handler

import (
	"context"

	"go.uber.org/zap"
	pb "server/pkg/pb"
)

func (h *WorldHandler) GetDisciples(ctx context.Context, req *pb.GetDisciplesRequest) (*pb.GetDisciplesResponse, error) {
	h.log.Info("GetDisciples request")
	
	return &pb.GetDisciplesResponse{
		Base: &pb.BaseResponse{Code: 0, Message: "msg_success"},
		Disciples: []*pb.Disciple{
			{Id: 101, DiscipleCode: "luc_tuyet_ky", Name: "Lục Tuyết Kỳ", Rarity: "SSR", Level: 1, Traits: []string{"hardworking", "cold"}},
		},
	}, nil
}

func (h *WorldHandler) LevelUpDisciple(ctx context.Context, req *pb.LevelUpDiscipleRequest) (*pb.LevelUpDiscipleResponse, error) {
	h.log.Info("LevelUpDisciple request", zap.Int64("id", req.DiscipleId))
	
	return &pb.LevelUpDiscipleResponse{
		Base: &pb.BaseResponse{Code: 0, Message: "msg_levelup_success"},
	}, nil
}

func (h *WorldHandler) LevelUpHero(ctx context.Context, req *pb.LevelUpHeroRequest) (*pb.LevelUpHeroResponse, error) {
	h.log.Info("LevelUpHero request", zap.Int64("id", req.HeroId))
	
	return &pb.LevelUpHeroResponse{
		Base: &pb.BaseResponse{Code: 0, Message: "msg_levelup_hero_success"},
		Hero: &pb.Hero{Id: req.HeroId, Name: "Hero Upgraded", Level: 2, Rarity: "SSR", Power: 1000},
	}, nil
}
