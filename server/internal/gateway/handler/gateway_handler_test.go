package handler

import (
	"context"
	"testing"

	"go.uber.org/zap"
	"google.golang.org/grpc"
	pb "server/pkg/pb"
)

type MockWorldClient struct {
	pb.WorldServiceClient
	pb.GatewayServiceClient
}

func (m *MockWorldClient) GetPlayerProfile(ctx context.Context, in *pb.GetPlayerProfileRequest, opts ...grpc.CallOption) (*pb.GetPlayerProfileResponse, error) {
	return &pb.GetPlayerProfileResponse{
		Base: &pb.BaseResponse{Code: 0, Message: "msg_success_mock"},
	}, nil
}

func (m *MockWorldClient) CreatePlayer(ctx context.Context, in *pb.CreatePlayerRequest, opts ...grpc.CallOption) (*pb.CreatePlayerResponse, error) {
	return &pb.CreatePlayerResponse{
		Base: &pb.BaseResponse{Code: 0, Message: "msg_create_success_mock"},
	}, nil
}

func (m *MockWorldClient) Close() {}

type MockCombatClient struct {
	pb.CombatServiceClient
}

func (m *MockCombatClient) Close() {}

func TestGatewayHandler(t *testing.T) {
	mockWorld := &MockWorldClient{}
	mockCombat := &MockCombatClient{}
	log := zap.NewNop()
	h := New(mockWorld, mockCombat, log)

	ctx := context.Background()

	t.Run("GetPlayerProfile", func(t *testing.T) {
		resp, err := h.GetPlayerProfile(ctx, &pb.GetPlayerProfileRequest{})
		if err != nil {
			t.Fatalf("GetPlayerProfile failed: %v", err)
		}
		if resp.Base.Message != "msg_success_mock" {
			t.Errorf("Expected msg_success_mock, got %s", resp.Base.Message)
		}
	})

	t.Run("CreatePlayer", func(t *testing.T) {
		resp, err := h.CreatePlayer(ctx, &pb.CreatePlayerRequest{})
		if err != nil {
			t.Fatalf("CreatePlayer failed: %v", err)
		}
		if resp.Base.Message != "msg_create_success_mock" {
			t.Errorf("Expected msg_create_success_mock, got %s", resp.Base.Message)
		}
	})
}
