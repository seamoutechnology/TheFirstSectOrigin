using System.Collections;
using System.Linq;
using UnityEngine;
using GameClient.Gameplay.Combat.Skills;

namespace GameClient.Gameplay.Combat.States
{
    public class PlayerTurnState : ICombatState
    {
        public IEnumerator Enter(CombatManager manager)
        {
            var activeEntity = manager.CurrentActiveEntity;
            Debug.Log($"[Combat] Đến lượt người chơi: {activeEntity.entityName}");
            yield return new WaitForSeconds(0.8f);

            var aliveEnemies = manager.Enemies.Where(e => !e.IsDead).ToList();
            if (aliveEnemies.Count == 0)
            {
                manager.NextTurn();
                yield break;
            }

            // Mặc định: Đánh Thường
            SkillData skillToUse = ScriptableObject.CreateInstance<SkillData>();
            skillToUse.SkillID = "basic_attack";
            skillToUse.SkillName = "Đánh Thường";
            skillToUse.PrimaryEffect = SkillEffectType.Damage;
            skillToUse.PrimaryMultiplier = 1.0f;

            // Nếu nhân vật có kỹ năng cấu hình từ server, chọn ngẫu nhiên một kỹ năng đủ MP/Cooldown
            if (activeEntity.skills != null && activeEntity.skills.Count > 0)
            {
                var castableSkills = activeEntity.skills.Where(s => !s.IsLocked).ToList();
                if (castableSkills.Count > 0)
                {
                    // Random chọn 1 kỹ năng hoặc đánh thường
                    var chosenPbSkill = castableSkills[Random.Range(0, castableSkills.Count)];
                    
                    skillToUse.SkillID = chosenPbSkill.SkillCode;
                    skillToUse.SkillName = chosenPbSkill.Name;
                    skillToUse.PrimaryMultiplier = chosenPbSkill.DamageMultiplier;
                    
                    // Bản đồ hóa EffectType string sang SkillEffectType enum
                    string effType = chosenPbSkill.EffectType.ToLower();
                    if (effType == "heal")
                    {
                        skillToUse.PrimaryEffect = SkillEffectType.Heal;
                        skillToUse.IsSupport = true;
                    }
                    else if (effType == "buff")
                    {
                        skillToUse.PrimaryEffect = SkillEffectType.BuffDefense;
                        skillToUse.PrimaryFlatBonus = 50;
                        skillToUse.IsSupport = true;
                    }
                    else
                    {
                        skillToUse.PrimaryEffect = SkillEffectType.Damage;
                        skillToUse.IsSupport = false;
                    }
                    
                    // Nếu là skill đặc biệt, tiêu hao ít MP
                    skillToUse.MPCost = chosenPbSkill.Cooldown > 0 ? 30 : 0;
                    
                    // Kiểm tra điều kiện MP
                    if (activeEntity.currentMP < skillToUse.MPCost)
                    {
                        // Rollback về đánh thường
                        skillToUse.SkillID = "basic_attack";
                        skillToUse.SkillName = "Đánh Thường";
                        skillToUse.PrimaryEffect = SkillEffectType.Damage;
                        skillToUse.PrimaryMultiplier = 1.0f;
                        skillToUse.IsSupport = false;
                        skillToUse.MPCost = 0;
                        activeEntity.AddMP(25);
                    }
                }
                else
                {
                    activeEntity.AddMP(25);
                }
            }
            else
            {
                // Quyết định kỹ năng dựa trên lượng HP và MP của nhân vật (Fallback cũ)
                float hpPercent = (float)activeEntity.currentHP / activeEntity.maxHP;

                if (hpPercent < 0.4f && activeEntity.currentMP >= 40 && Random.value < 0.6f)
                {
                    // Hồi Huyết Thuật
                    skillToUse.SkillID = "hoi_huyet";
                    skillToUse.SkillName = "Hồi Huyết Thuật";
                    skillToUse.IsSupport = true;
                    skillToUse.PrimaryEffect = SkillEffectType.Heal;
                    skillToUse.PrimaryMultiplier = 1.5f;
                    skillToUse.MPCost = 40;
                }
                else if (activeEntity.currentMP >= 50 && Random.value < 0.5f)
                {
                    // Vạn Kiếm Quyết (AOE)
                    skillToUse.SkillID = "van_kiem_quyet";
                    skillToUse.SkillName = "Vạn Kiếm Quyết";
                    skillToUse.IsAoE = true;
                    skillToUse.PrimaryEffect = SkillEffectType.Damage;
                    skillToUse.PrimaryMultiplier = 0.8f;
                    skillToUse.MPCost = 50;
                }
                else
                {
                    // Đánh thường tích lũy MP
                    activeEntity.AddMP(25);
                }
            }

            CombatEntity target = null;
            if (skillToUse.IsSupport)
            {
                // Chọn đồng đội có HP thấp nhất để hỗ trợ/trị liệu
                target = manager.Players.Where(p => !p.IsDead).OrderBy(p => p.currentHP).FirstOrDefault();
                if (target == null) target = activeEntity;
            }
            else
            {
                // Tấn công quái vật ngẫu nhiên
                target = aliveEnemies[Random.Range(0, aliveEnemies.Count)];
            }

            manager.ExecuteAction(skillToUse, target);
        }

        public void Execute(CombatManager manager)
        {
        }

        public IEnumerator Exit(CombatManager manager)
        {
            yield break;
        }
    }
}
