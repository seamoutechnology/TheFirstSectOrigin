package handler

import (
	"context"
	"encoding/json"
	"strings"

	"go.uber.org/zap"
	"server/internal/world/repository"
	pb "server/pkg/pb"
)

func (h *WorldHandler) GetDisciples(ctx context.Context, req *pb.GetDisciplesRequest) (*pb.GetDisciplesResponse, error) {
	h.log.Info("GetDisciples request")
	
	return &pb.GetDisciplesResponse{
		Base: &pb.BaseResponse{Code: 0, Message: "msg_success"},
		Disciples: []*pb.Disciple{
			{Id: 101, DiscipleCode: "luc_tuyet_ky", Name: "Lục Tuyết Kỳ", Rarity: "SSR", Level: 1, Traits: []string{"hardworking", "cold"}},
		},
	}, nil
}

func (h *WorldHandler) GetHeroes(ctx context.Context, req *pb.GetHeroesRequest) (*pb.GetHeroesResponse, error) {
	h.log.Info("GetHeroes request")
	userID, ok := h.getUserID(ctx)
	if !ok {
		return &pb.GetHeroesResponse{Base: &pb.BaseResponse{Code: 401, Message: "msg_unauthorized"}}, nil
	}

	heroes, code, msg := h.svc.GetHeroes(ctx, userID)
	h.log.Info("GetHeroes service returned", zap.Int64("userID", userID), zap.Int("count", len(heroes)), zap.Int32("code", code), zap.String("msg", msg))
	if code != 0 {
		return &pb.GetHeroesResponse{Base: &pb.BaseResponse{Code: code, Message: msg}}, nil
	}

	// Fetch all skill configs from DB
	configs, err := h.svc.GetSkillConfigs(ctx)
	configMap := make(map[string]*repository.SkillConfig)
	if err == nil {
		for _, cfg := range configs {
			configMap[cfg.SkillCode] = cfg
		}
	} else {
		h.log.Error("Failed to fetch skill configs", zap.Error(err))
	}

	type LearnedSkill struct {
		SkillCode string `json:"skill_code"`
		Level     int32  `json:"level"`
	}

	pbHeroes := make([]*pb.Hero, 0, len(heroes))
	for _, hero := range heroes {
		var traitsList []string
		if hero.Traits != "" {
			_ = json.Unmarshal([]byte(hero.Traits), &traitsList)
		}

		var learnedSkills []LearnedSkill
		if hero.Skills != "" && hero.Skills != "[]" {
			_ = json.Unmarshal([]byte(hero.Skills), &learnedSkills)
		}

		pbSkills := make([]*pb.HeroSkill, 0, len(learnedSkills))
		for _, ls := range learnedSkills {
			skillName := ls.SkillCode
			var dmgMult float32 = 1.0
			var cooldown int32 = 0
			var effType string = "damage"

			if cfg, found := configMap[ls.SkillCode]; found {
				skillName = cfg.Name
				dmgMult = float32(cfg.DamageMultiplier)
				cooldown = cfg.Cooldown
				effType = cfg.EffectType
			}

			pbSkills = append(pbSkills, &pb.HeroSkill{
				SkillCode:        ls.SkillCode,
				Name:             skillName,
				DamageMultiplier: dmgMult,
				Cooldown:         cooldown,
				EffectType:       effType,
				Level:            ls.Level,
			})
		}

		actualHP := hero.BaseHP + hero.Level*100
		actualATK := hero.BaseATK + hero.Level*10
		actualDEF := hero.BaseDEF + hero.Level*5
		pbHeroes = append(pbHeroes, &pb.Hero{
			Id:      hero.ID,
			Name:    hero.Name,
			Level:   hero.Level,
			Rarity:  hero.Rarity,
			Power:   int64(actualATK*10 + actualHP + actualDEF*5),
			Star:    hero.Star,
			Traits:  traitsList,
			Skills:  pbSkills,
			Element: strings.ToUpper(hero.Element),
		})
	}

	formationMap, _, _ := h.svc.GetFormation(ctx, userID)
	pbFormation := make([]*pb.FormationSlot, 0, len(formationMap))
	for pos, heroID := range formationMap {
		pbFormation = append(pbFormation, &pb.FormationSlot{
			Position:     pos,
			PlayerHeroId: heroID,
		})
	}

	return &pb.GetHeroesResponse{
		Base:      &pb.BaseResponse{Code: 0, Message: "msg_success"},
		Heroes:    pbHeroes,
		Formation: pbFormation,
	}, nil
}

func (h *WorldHandler) LevelUpDisciple(ctx context.Context, req *pb.LevelUpDiscipleRequest) (*pb.LevelUpDiscipleResponse, error) {
	h.log.Info("LevelUpDisciple request", zap.Int64("id", req.DiscipleId))
	
	return &pb.LevelUpDiscipleResponse{
		Base: &pb.BaseResponse{Code: 0, Message: "msg_levelup_success"},
	}, nil
}

func (h *WorldHandler) LevelUpHero(ctx context.Context, req *pb.LevelUpHeroRequest) (*pb.LevelUpHeroResponse, error) {
	h.log.Info("LevelUpHero request", zap.Int64("id", req.HeroId))
	userID, ok := h.getUserID(ctx)
	if !ok {
		return &pb.LevelUpHeroResponse{Base: &pb.BaseResponse{Code: 401, Message: "msg_unauthorized"}}, nil
	}

	hero, code, msg := h.svc.LevelUpHero(ctx, userID, req.HeroId)
	if code != 0 {
		return &pb.LevelUpHeroResponse{Base: &pb.BaseResponse{Code: code, Message: msg}}, nil
	}

	// Fetch all skill configs from DB to build the pb.Hero response
	configs, err := h.svc.GetSkillConfigs(ctx)
	configMap := make(map[string]*repository.SkillConfig)
	if err == nil {
		for _, cfg := range configs {
			configMap[cfg.SkillCode] = cfg
		}
	}

	var traitsList []string
	if hero.Traits != "" {
		_ = json.Unmarshal([]byte(hero.Traits), &traitsList)
	}

	type LearnedSkill struct {
		SkillCode string `json:"skill_code"`
		Level     int32  `json:"level"`
	}
	var learnedSkills []LearnedSkill
	if hero.Skills != "" && hero.Skills != "[]" {
		_ = json.Unmarshal([]byte(hero.Skills), &learnedSkills)
	}

	pbSkills := make([]*pb.HeroSkill, 0, len(learnedSkills))
	for _, ls := range learnedSkills {
		skillName := ls.SkillCode
		var dmgMult float32 = 1.0
		var cooldown int32 = 0
		var effType string = "damage"

		if cfg, found := configMap[ls.SkillCode]; found {
			skillName = cfg.Name
			dmgMult = float32(cfg.DamageMultiplier)
			cooldown = cfg.Cooldown
			effType = cfg.EffectType
		}

		pbSkills = append(pbSkills, &pb.HeroSkill{
			SkillCode:        ls.SkillCode,
			Name:             skillName,
			DamageMultiplier: dmgMult,
			Cooldown:         cooldown,
			EffectType:       effType,
			Level:            ls.Level,
		})
	}

	actualHP := hero.BaseHP + hero.Level*100
	actualATK := hero.BaseATK + hero.Level*10
	actualDEF := hero.BaseDEF + hero.Level*5
	return &pb.LevelUpHeroResponse{
		Base: &pb.BaseResponse{Code: 0, Message: "msg_levelup_hero_success"},
		Hero: &pb.Hero{
			Id:      hero.ID,
			Name:    hero.Name,
			Level:   hero.Level,
			Rarity:  hero.Rarity,
			Power:   int64(actualATK*10 + actualHP + actualDEF*5),
			Star:    hero.Star,
			Traits:  traitsList,
			Skills:  pbSkills,
			Element: strings.ToUpper(hero.Element),
		},
	}, nil
}
