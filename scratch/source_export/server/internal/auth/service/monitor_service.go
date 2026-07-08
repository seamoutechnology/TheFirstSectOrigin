package service

import (
	"context"
	"fmt"
	"math/rand"
	"sync"
	"time"

	"go.uber.org/zap"
	"server/internal/auth/repository"
	"server/pkg/docker"
)

type MonitorService struct {
	zoneRepo repository.IZoneRepository
	log      *zap.Logger
	mu       sync.Mutex

	// We can use this to simulate high load via API calls
	simulatedCpuSpike map[int]float64
}

func NewMonitorService(zoneRepo repository.IZoneRepository, log *zap.Logger) *MonitorService {
	return &MonitorService{
		zoneRepo:          zoneRepo,
		log:               log,
		simulatedCpuSpike: make(map[int]float64),
	}
}

func (s *MonitorService) SetSimulatedCpuSpike(zoneID int, val float64) {
	s.mu.Lock()
	defer s.mu.Unlock()
	s.simulatedCpuSpike[zoneID] = val
}

func (s *MonitorService) GetSimulatedCpuSpike(zoneID int) float64 {
	s.mu.Lock()
	defer s.mu.Unlock()
	return s.simulatedCpuSpike[zoneID]
}

func (s *MonitorService) Start(ctx context.Context) {
	ticker := time.NewTicker(5 * time.Second) // Poll frequently for quicker testing feedback
	s.log.Info("Starting Server Monitor & Auto-Scaling Service...")

	go func() {
		for {
			select {
			case <-ctx.Done():
				ticker.Stop()
				return
			case <-ticker.C:
				s.pollAndScale(ctx)
			}
		}
	}()
}

func (s *MonitorService) pollAndScale(ctx context.Context) {
	zones, err := s.zoneRepo.FindActive(ctx)
	if err != nil {
		s.log.Error("Failed to query active zones", zap.Error(err))
		return
	}

	if len(zones) == 0 {
		return
	}

	// 1. Update metrics for each zone
	for _, zone := range zones {
		// Calculate simulated metrics
		spike := s.GetSimulatedCpuSpike(zone.ID)
		
		cpu := spike
		if cpu == 0 {
			// Normal idle load
			cpu = 15.0 + rand.Float64()*10.0
		}
		
		ram := 20.0 + rand.Float64()*8.0
		players := zone.PlayerCount
		if players == 0 {
			players = int(cpu / 2.0)
		}

		// Update database
		err := s.zoneRepo.UpdateMetrics(ctx, zone.ID, cpu, ram, players)
		if err != nil {
			s.log.Warn("Failed to update metrics for zone", zap.Int("zone_id", zone.ID), zap.Error(err))
		}
	}

	// 2. Check the last zone (newest server) for auto-scaling
	lastZone := zones[len(zones)-1]
	currentCpu := s.GetSimulatedCpuSpike(lastZone.ID)
	if currentCpu == 0 {
		// Query the value from DB since we updated it above
		latestZones, err := s.zoneRepo.FindActive(ctx)
		if err == nil && len(latestZones) > 0 {
			lastZone = latestZones[len(latestZones)-1]
			currentCpu = lastZone.CPUUsage
		}
	}

	s.log.Info("Checking last zone load", zap.String("zone", lastZone.Name), zap.Float64("cpu", currentCpu))

	// If the CPU of the newest zone exceeds 80%, spawn a new one!
	if currentCpu >= 80.0 {
		s.log.Warn("High resource usage detected! Triggering autoscaling...", zap.String("zone", lastZone.Name), zap.Float64("cpu", currentCpu))
		
		count, err := s.zoneRepo.GetCount(ctx)
		if err != nil {
			s.log.Error("Failed to get total zone count", zap.Error(err))
			return
		}

		nextName := s.GetNextZoneName(count)

		// Spawn new zone containers via Docker SDK
		zoneID := count + 1
		s.log.Info("Spawning Docker containers for new zone", zap.Int("zone_id", zoneID), zap.String("name", nextName))
		gatewayURL, err := docker.SpawnZoneContainers(ctx, zoneID)
		if err != nil {
			s.log.Error("Autoscaling failed: could not spawn Docker containers", zap.String("name", nextName), zap.Error(err))
			return
		}

		err = s.zoneRepo.CreateZone(ctx, nextName, "normal", gatewayURL)
		if err != nil {
			s.log.Error("Autoscaling failed: could not create new zone in DB", zap.String("name", nextName), zap.Error(err))
		} else {
			s.log.Info("Autoscaling success! Provisioned new server", zap.String("name", nextName), zap.String("url", gatewayURL))
			// Clear simulated spike of previous last zone
			s.SetSimulatedCpuSpike(lastZone.ID, 0)
		}
	}
}

func (s *MonitorService) GetNextZoneName(count int) string {
	// 1-40: Thanh Long 1->40
	// 41-80: Bạch Hổ 1->40
	// 81-120: Chu Tước 1->40
	// 121-160: Huyền Vũ 1->40
	if count < 40 {
		return fmt.Sprintf("Thanh Long %d", count+1)
	} else if count < 80 {
		return fmt.Sprintf("Bạch Hổ %d", count-39)
	} else if count < 120 {
		return fmt.Sprintf("Chu Tước %d", count-79)
	} else {
		return fmt.Sprintf("Huyền Vũ %d", count-119)
	}
}
