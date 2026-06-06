package service

import (
	"context"
	"testing"
	"server/internal/world/repository"
)

type MockPlayerRepo struct {
	repository.IPlayerRepository
	Players map[int64]*repository.Player
}

func (m *MockPlayerRepo) FindByUserID(ctx context.Context, userID int64, serverID string) (*repository.Player, error) {
	if p, ok := m.Players[userID]; ok && p.ServerID == serverID {
		return p, nil
	}
	return nil, repository.ErrNotFound
}

func (m *MockPlayerRepo) Create(ctx context.Context, userID int64, serverID string, nickname string) (*repository.Player, error) {
	p := &repository.Player{
		ID:       int64(len(m.Players) + 1),
		UserID:   userID,
		ServerID: serverID,
		Nickname: nickname,
	}
	m.Players[userID] = p
	return p, nil
}

func (m *MockPlayerRepo) InitPlayerBuildings(ctx context.Context, playerID int64) error {
	return nil
}

func (m *MockPlayerRepo) SetFormation(ctx context.Context, playerID int64, slots map[int32]int64) error {
	return nil
}

func (m *MockPlayerRepo) GetVersionConfig(ctx context.Context, platform string) (*repository.VersionConfig, error) {
	return &repository.VersionConfig{
		Platform:           platform,
		ClientVersion:      "1.0.0",
		AddressableVersion: "v1",
	}, nil
}

func TestWorldService_Player(t *testing.T) {
	repo := &MockPlayerRepo{Players: make(map[int64]*repository.Player)}
	svc := New(repo, "zone1")
	ctx := context.Background()

	t.Run("CreatePlayer", func(t *testing.T) {
		p, code, msg := svc.CreatePlayer(ctx, 1, "Tester")
		if code != ErrCodeSuccess {
			t.Errorf("Expected success, got %d: %s", code, msg)
		}
		if p.Nickname != "Tester" {
			t.Errorf("Expected nickname Tester, got %s", p.Nickname)
		}
	})

	t.Run("CreateDuplicate", func(t *testing.T) {
		_, code, _ := svc.CreatePlayer(ctx, 1, "Tester2")
		if code != ErrCodeAlreadyExist {
			t.Errorf("Expected AlreadyExist, got %d", code)
		}
	})

	t.Run("GetProfile", func(t *testing.T) {
		p, code, _ := svc.GetPlayerProfile(ctx, 1)
		if code != ErrCodeSuccess {
			t.Errorf("Expected success, got %d", code)
		}
		if p.UserID != 1 {
			t.Errorf("Expected UserID 1, got %d", p.UserID)
		}
	})
}
