package base

import (
	"math/rand"
	"time"
)

type BaseState struct {
	Inventory      map[string]int64
	TroopAtkBonus  float64
	HasAutoHarvest bool // Đăng ký gói Tự động thu hoạch
}

func ProcessProductionCycle(layout *BaseLayout, state *BaseState, now time.Time) {
	state.TroopAtkBonus = 0

	for i := range layout.Buildings {
		b := &layout.Buildings[i]
		cfg, exists := Database[b.ID]
		if !exists {
			continue
		}

		if b.Storage == nil {
			b.Storage = make(map[string]int64)
		}
		if b.UpgradeBranches == nil {
			b.UpgradeBranches = make(map[string]int)
		}
		if b.LastHarvestTime == 0 {
			b.LastHarvestTime = now.Unix()
		}

		if cfg.EffectType == EffectResourceGen {
			speedLvl := float64(b.UpgradeBranches[BranchSpeed])
			actualInterval := float64(cfg.BaseInterval) * (1.0 - (speedLvl * 0.05)) // Giảm 5% mỗi cấp
			if actualInterval < 1 {
				actualInterval = 1
			}

			elapsed := float64(now.Unix() - b.LastHarvestTime)
			cycles := int(elapsed / actualInterval)

			if cycles > 0 {
				yieldLvl := b.UpgradeBranches[BranchYield]
				qualityLvl := b.UpgradeBranches[BranchQuality]

				var totalMain int64 = 0
				var totalRare int64 = 0

				for c := 0; c < cycles; c++ {
					yieldMultiplier := int64(1)
					if rand.Float64() < float64(yieldLvl)*0.02 { // 2% tỷ lệ x2 mỗi cấp
						yieldMultiplier = 2
					}

					totalMain += cfg.BaseYield * yieldMultiplier

					if cfg.RareProduct != "" && rand.Float64() < float64(qualityLvl)*0.01 { // 1% tỷ lệ rớt đồ xịn mỗi cấp
						totalRare += 1 * yieldMultiplier
					}
				}

				if state.HasAutoHarvest {
					if state.Inventory == nil {
						state.Inventory = make(map[string]int64)
					}
					state.Inventory[cfg.MainProduct] += totalMain
					if totalRare > 0 {
						state.Inventory[cfg.RareProduct] += totalRare
					}
					b.LastHarvestTime = now.Unix() // Reset
				} else {
					currentTotal := b.Storage[cfg.MainProduct]
					if currentTotal < cfg.MaxCapacity {
						added := totalMain
						if currentTotal+added > cfg.MaxCapacity {
							added = cfg.MaxCapacity - currentTotal
						}
						b.Storage[cfg.MainProduct] += added
					}

					b.Storage[cfg.RareProduct] += totalRare
					
					b.LastHarvestTime += int64(float64(cycles) * actualInterval)
				}
			}
		} else if cfg.EffectType == EffectStatBoost {
		}
	}
}
