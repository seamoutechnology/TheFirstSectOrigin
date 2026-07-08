package handler

import (
	"context"
	"testing"
	"time"

	"go.uber.org/zap"
	"google.golang.org/grpc/metadata"
	"server/internal/world/repository"
	"server/internal/world/service"
	pb "server/pkg/pb"
)

type MockPlayerRepo struct {
	repository.IPlayerRepository
}

func (m *MockPlayerRepo) FindByUserID(ctx context.Context, userID int64, serverID string) (*repository.Player, error) {
	return &repository.Player{
		ID:       1,
		UserID:   userID,
		ServerID: serverID,
		Nickname: "TestPlayer",
		Level:    10,
		Gold:     1000,
		Diamond:  10000,
	}, nil
}

func (m *MockPlayerRepo) GetActiveBanners(ctx context.Context) ([]*repository.GachaBanner, error) {
	endTime := time.Now().Add(24 * time.Hour)
	return []*repository.GachaBanner{
		{
			ID:          1,
			Name:        "Test Banner",
			Description: "Test Description",
			CostDiamond: 100,
			EndAt:       &endTime,
			PityCount:   80,
		},
	}, nil
}

func (m *MockPlayerRepo) GetGachaHeroPool(ctx context.Context) ([]*repository.HeroTemplate, error) {
	return []*repository.HeroTemplate{
		{
			Code:      "FIRE_WARRIOR_01",
			Name:      "Fire Warrior",
			Rarity:    "SR",
			Element:   "FIRE",
			Role:        "WARRIOR",
			BaseHP:      100,
			BaseATK:     10,
			BaseDEF:     5,
			BaseSpeed:   10,
			GachaWeight: 100,
		},
	}, nil
}

func (m *MockPlayerRepo) GetOrCreatePity(ctx context.Context, playerID int64, bannerID int32) (int32, error) {
	return 0, nil
}

func (m *MockPlayerRepo) AddHero(ctx context.Context, playerID int64, heroCode string) (*repository.PlayerHero, error) {
	return &repository.PlayerHero{
		ID:       10,
		PlayerID: playerID,
		HeroCode: heroCode,
		Level:    1,
		Star:     1,
		Exp:      0,
	}, nil
}

func (m *MockPlayerRepo) UpdatePity(ctx context.Context, playerID int64, bannerID int32, pityCount int32) error {
	return nil
}

func (m *MockPlayerRepo) UpdateResources(ctx context.Context, playerID int64, goldDelta int64, diamondDelta int64) (*repository.Player, error) {
	return &repository.Player{
		ID:      playerID,
		UserID:  123,
		Gold:    1000 + goldDelta,
		Diamond: 10000 + diamondDelta,
	}, nil
}

func TestWorldHandler_Integration(t *testing.T) {
	log := zap.NewNop()
	repo := &MockPlayerRepo{}
	svc := service.New(repo, "zone1")
	h := New(svc, log)

	t.Run("TestGetPlayerProfile", func(t *testing.T) {
		ctx := metadata.NewIncomingContext(context.Background(), metadata.Pairs("user-id", "123"))
		resp, err := h.GetPlayerProfile(ctx, &pb.GetPlayerProfileRequest{})
		if err != nil {
			t.Errorf("GetPlayerProfile failed: %v", err)
		}
		if resp.Base.Code != 0 {
			t.Errorf("Expected code 0, got %d", resp.Base.Code)
		}
		if resp.Base.Message != "msg_success" {
			t.Errorf("Expected message_id msg_success, got %s", resp.Base.Message)
		}
	})

	t.Run("TestDoGacha", func(t *testing.T) {
		ctx := metadata.NewIncomingContext(context.Background(), metadata.Pairs("user-id", "123"))
		resp, err := h.DoGacha(ctx, &pb.DoGachaRequest{BannerId: 1, Count: 1})
		if err != nil {
			t.Fatalf("DoGacha failed: %v", err)
		}
		if resp.Base.Code != 0 {
			t.Errorf("Expected code 0, got %d, message: %s", resp.Base.Code, resp.Base.Message)
		}
		if resp.Base.Message != "msg_gacha_success" {
			t.Errorf("Expected gacha success ID, got %s", resp.Base.Message)
		}
	})
}
