package handler

import (
	"context"
	"testing"

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
		Diamond:  100,
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
		resp, err := h.DoGacha(context.Background(), &pb.DoGachaRequest{BannerId: 1, Count: 1})
		if err != nil {
			t.Errorf("DoGacha failed: %v", err)
		}
		if resp.Base.Message != "msg_gacha_success" {
			t.Errorf("Expected gacha success ID, got %s", resp.Base.Message)
		}
	})
}
