package combat

import (
	"math"
)

type CombatEngine struct{}

func NewCombatEngine() *CombatEngine {
	return &CombatEngine{}
}

func (e *CombatEngine) CheckBattleEnd(state *BattleState) (bool, string) {
	if state.Turn > state.Config.MaxTurns {
		if state.Config.WinCondition == "DEAL_DAMAGE" {
			return true, "WIN" // Chế độ World Boss: hết lượt là tính điểm thắng
		}
		return true, "LOSS" // Các chế độ khác: hết lượt mà chưa xong là thua
	}

	playerAlive := 0
	enemyAlive := 0
	for _, u := range state.Units {
		if u.HP > 0 {
			if u.IsEnemy {
				enemyAlive++
			} else {
				playerAlive++
			}
		}
	}

	if playerAlive == 0 {
		return true, "LOSS"
	}

	if enemyAlive == 0 && state.Config.WinCondition == "KILL_ALL" {
		return true, "WIN"
	}

	return false, ""
}

func (e *CombatEngine) FindTarget(source *Unit, units []*Unit) *Unit {
	var bestTarget *Unit
	maxScore := -999999.0

	for _, t := range units {
		if t.HP <= 0 || t.IsEnemy == source.IsEnemy {
			continue
		}

		score := 0.0
		for _, rule := range source.TargetingRules {
			score += e.evaluateRule(source, t, rule)
		}

		if score > maxScore {
			maxScore = score
			bestTarget = t
		}
	}
	return bestTarget
}

func (e *CombatEngine) evaluateRule(source *Unit, target *Unit, rule TargetingRule) float64 {
	switch rule.Type {
	case "FROZEN":
		if target.IsStunned {
			return rule.Weight
		}
	case "BACKLINE":
		isBackline := (target.IsEnemy && target.Pos.X >= 2) || (!target.IsEnemy && target.Pos.X <= 1)
		if isBackline {
			return rule.Weight
		}
	case "LOWEST_HP":
		hpPercent := float64(target.HP) / float64(target.MaxHP)
		return rule.Weight * (1.0 - hpPercent)
	case "DISTANCE":
		dist := math.Sqrt(math.Pow(float64(target.Pos.X-source.Pos.X), 2) + math.Pow(float64(target.Pos.Y-source.Pos.Y), 2))
		return rule.Weight * dist // Thường weight ở đây sẽ âm
	}
	return 0
}
