package repository

import (
	"context"
)

type MockUserRepo struct {
	Users map[string]*User
}

func (m *MockUserRepo) Create(ctx context.Context, username, email, passwordHash string) (*User, error) {
	u := &User{
		ID:           int64(len(m.Users) + 1),
		Username:     username,
		Email:        email,
		PasswordHash: passwordHash,
	}
	if m.Users == nil {
		m.Users = make(map[string]*User)
	}
	m.Users[username] = u
	return u, nil
}

func (m *MockUserRepo) FindByEmail(ctx context.Context, email string) (*User, error) {
	for _, u := range m.Users {
		if u.Email == email {
			return u, nil
		}
	}
	return nil, ErrNotFound
}

func (m *MockUserRepo) FindByUsername(ctx context.Context, username string) (*User, error) {
	if u, ok := m.Users[username]; ok {
		return u, nil
	}
	return nil, ErrNotFound
}

type MockZoneRepo struct {
	Zones []*Zone
}

func (m *MockZoneRepo) FindActive(ctx context.Context) ([]*Zone, error) {
	return m.Zones, nil
}

func (m *MockZoneRepo) UpdateMetrics(ctx context.Context, zoneID int, cpu, ram float64, players int) error {
	return nil
}

func (m *MockZoneRepo) CreateZone(ctx context.Context, name, status, gatewayURL string) error {
	m.Zones = append(m.Zones, &Zone{
		ID:         len(m.Zones) + 1,
		Name:       name,
		Status:     status,
		GatewayURL: gatewayURL,
		IsActive:   true,
	})
	return nil
}

func (m *MockZoneRepo) GetCount(ctx context.Context) (int, error) {
	return len(m.Zones), nil
}
