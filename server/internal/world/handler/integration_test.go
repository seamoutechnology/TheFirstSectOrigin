package handler

import (
	"context"
	"testing"

	"go.uber.org/zap"
	pb "server/pkg/pb"
	"server/internal/world/service"
)

func TestWorldHandler_Integration(t *testing.T) {
	log := zap.NewNop()
	svc := service.New(nil, "zone1") // Repo = nil cho đơn giản
	h := New(svc, log)

	t.Run("TestGetPlayerProfile", func(t *testing.T) {
		resp, err := h.GetPlayerProfile(context.Background(), &pb.GetPlayerProfileRequest{})
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
