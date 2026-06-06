package service

import (
	"context"

	"server/pkg/pb"
	"go.uber.org/zap"
)

type CombatService struct {
	log *zap.Logger
}

func NewCombatService(log *zap.Logger) *CombatService {
	return &CombatService{
		log: log,
	}
}

func (s *CombatService) ValidatePvEResult(ctx context.Context, req *pb.ValidatePvEResultRequest) (*pb.ValidatePvEResultResponse, error) {
	s.log.Info("Validating PvE result", zap.String("enemy", req.EnemyId), zap.Int32("player_power", req.PlayerPower), zap.Int32("enemy_power", req.EnemyPower), zap.Bool("is_victory", req.IsVictory))

	if !req.IsVictory {
		return &pb.ValidatePvEResultResponse{
			Base:    &pb.BaseResponse{Code: 0, Message: "Defeat accepted"},
			IsValid: true,
		}, nil
	}

	minRequiredPower := float64(req.EnemyPower) * 0.5
	if float64(req.PlayerPower) < minRequiredPower {
		s.log.Warn("Suspicious combat result detected, attempting soft validation on logs", zap.Int("log_count", len(req.CombatLogs)))
		
		isValid := s.verifyCombatLog(req.CombatLogs, req.PlayerPower, req.EnemyPower)
		if !isValid {
			return &pb.ValidatePvEResultResponse{
				Base:    &pb.BaseResponse{Code: 1, Message: "Suspicious activity detected! Hack verification failed."},
				IsValid: false,
			}, nil
		}
		s.log.Info("Soft validation passed for weak player")
	}

	return &pb.ValidatePvEResultResponse{
		Base:            &pb.BaseResponse{Code: 0, Message: "Victory valid"},
		IsValid:         true,
		RewardExp:       100,
		RewardLinhThach: 50,
	}, nil
}

func (s *CombatService) verifyCombatLog(logs []*pb.CombatActionLog, playerPower int32, enemyPower int32) bool {
	if len(logs) == 0 {
		return false // Thắng mà không có action nào thì là hack chắc chắn
	}

	totalDamage := int32(0)
	
	maxPossibleHitDamage := playerPower * 2 

	for _, log := range logs {
		if log.Damage > 0 {
			totalDamage += log.Damage
			if log.Damage > maxPossibleHitDamage {
				s.log.Warn("Hack detected: Impossible damage in single hit", zap.Int32("damage", log.Damage), zap.Int32("max_allowed", maxPossibleHitDamage))
				return false
			}
		}
	}

	estimatedEnemyHP := enemyPower / 2
	if totalDamage < estimatedEnemyHP {
		s.log.Warn("Hack detected: Total damage dealt is less than enemy HP", zap.Int32("total_damage", totalDamage), zap.Int32("enemy_hp", estimatedEnemyHP))
	}

	return true
}
