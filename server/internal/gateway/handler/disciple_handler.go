package handler

import (
	"context"
	pb "server/pkg/pb"
)

func (h *GatewayHandler) GetHeroes(ctx context.Context, req *pb.GetHeroesRequest) (*pb.GetHeroesResponse, error) {
	return h.world.GetHeroes(ctx, req)
}

func (h *GatewayHandler) SetFormation(ctx context.Context, req *pb.SetFormationRequest) (*pb.SetFormationResponse, error) {
	return h.world.SetFormation(ctx, req)
}

func (h *GatewayHandler) LevelUpDisciple(ctx context.Context, req *pb.LevelUpDiscipleRequest) (*pb.LevelUpDiscipleResponse, error) {
	return h.world.LevelUpDisciple(ctx, req)
}

func (h *GatewayHandler) LevelUpHero(ctx context.Context, req *pb.LevelUpHeroRequest) (*pb.LevelUpHeroResponse, error) {
	return h.world.LevelUpHero(ctx, req)
}
