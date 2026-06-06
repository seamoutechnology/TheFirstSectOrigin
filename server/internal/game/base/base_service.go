package base

import (
	"crypto/rand"
	"fmt"
	"math/big"
	"sync"
	"time"
)

type BaseService struct {
	playerBases map[string]BaseLayout
	
	shareCodes map[string]ShareCodeRecord

	mu sync.RWMutex
}

func NewBaseService() *BaseService {
	return &BaseService{
		playerBases: make(map[string]BaseLayout),
		shareCodes:  make(map[string]ShareCodeRecord),
	}
}

func (s *BaseService) SaveLayout(playerID string, layout BaseLayout) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	if err := ValidatePlacement(layout); err != nil {
		return fmt.Errorf("placement validation failed: %v", err)
	}

	s.playerBases[playerID] = layout
	return nil
}

func (s *BaseService) GenerateShareCode(playerID string) (string, error) {
	s.mu.Lock()
	defer s.mu.Unlock()

	layout, exists := s.playerBases[playerID]
	if !exists {
		return "", fmt.Errorf("player %s has no base layout to share", playerID)
	}

	pinCode := generateRandomPIN()

	s.shareCodes[pinCode] = ShareCodeRecord{
		PINCode:   pinCode,
		Layout:    layout,
		OwnerID:   playerID,
		CreatedAt: time.Now().Unix(),
	}

	return pinCode, nil
}

func (s *BaseService) ImportShareCode(playerID string, pinCode string) (BaseLayout, error) {
	s.mu.RLock()
	record, exists := s.shareCodes[pinCode]
	s.mu.RUnlock()

	if !exists {
		return BaseLayout{}, fmt.Errorf("invalid share code")
	}

	err := s.SaveLayout(playerID, record.Layout)
	if err != nil {
		return BaseLayout{}, err
	}

	return record.Layout, nil
}

func (s *BaseService) UpgradeBuildingBranch(playerID string, buildingID string, branchName string) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	layout, exists := s.playerBases[playerID]
	if !exists {
		return fmt.Errorf("player base not found")
	}

	for i := range layout.Buildings {
		b := &layout.Buildings[i]
		if b.ID == buildingID {
			if b.UpgradeBranches == nil {
				b.UpgradeBranches = make(map[string]int)
			}
			
			// FIXME: Check tài nguyên trừ tiền ở đây
			b.UpgradeBranches[branchName]++
			return nil
		}
	}
	return fmt.Errorf("building %s not found in base", buildingID)
}

func (s *BaseService) HarvestBuilding(playerID string, buildingID string, state *BaseState) error {
	s.mu.Lock()
	defer s.mu.Unlock()

	layout, exists := s.playerBases[playerID]
	if !exists {
		return fmt.Errorf("player base not found")
	}

	for i := range layout.Buildings {
		b := &layout.Buildings[i]
		if b.ID == buildingID {
			if len(b.Storage) == 0 {
				return fmt.Errorf("storage is empty")
			}

			if state.Inventory == nil {
				state.Inventory = make(map[string]int64)
			}

			for itemKey, amount := range b.Storage {
				state.Inventory[itemKey] += amount
			}

			b.Storage = make(map[string]int64)
			
			return nil
		}
	}
	return fmt.Errorf("building %s not found in base", buildingID)
}

type HarvestResult int

const (
	ResultSuccess HarvestResult = iota
	ResultSyncIssue
	ResultCheatDetected
)

func (s *BaseService) BatchHarvestBuilding(playerID string, buildingIDs []string, state *BaseState) (HarvestResult, error) {
	s.mu.Lock()
	defer s.mu.Unlock()

	layout, exists := s.playerBases[playerID]
	if !exists {
		return ResultCheatDetected, fmt.Errorf("player base not found")
	}

	totalHarvested := 0

	for _, reqID := range buildingIDs {
		found := false
		for i := range layout.Buildings {
			b := &layout.Buildings[i]
			if b.ID == reqID {
				found = true
				if len(b.Storage) == 0 {
					return ResultCheatDetected, fmt.Errorf("cheat detected: building %s has no resources", reqID)
				}

				if state.Inventory == nil {
					state.Inventory = make(map[string]int64)
				}

				for itemKey, amount := range b.Storage {
					state.Inventory[itemKey] += amount
				}

				b.Storage = make(map[string]int64)
				totalHarvested++
				break
			}
		}
		if !found {
			return ResultCheatDetected, fmt.Errorf("cheat detected: building %s not on map", reqID)
		}
	}

	if totalHarvested < len(buildingIDs) {
		return ResultSyncIssue, nil
	}

	return ResultSuccess, nil
}

func generateRandomPIN() string {
	max := big.NewInt(999999)
	n, _ := rand.Int(rand.Reader, max)
	return fmt.Sprintf("%06d", n.Int64())
}
