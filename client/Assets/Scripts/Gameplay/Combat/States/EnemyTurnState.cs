using System.Collections;
using System.Linq;
using UnityEngine;
using GameClient.Gameplay.Combat.Skills;

namespace GameClient.Gameplay.Combat.States
{
    public class EnemyTurnState : ICombatState
    {
        public IEnumerator Enter(CombatManager manager)
        {
            var activeEntity = manager.CurrentActiveEntity;
            Debug.Log($"[Combat] Đến lượt kẻ địch: {activeEntity.entityName}");
            yield return new WaitForSeconds(0.8f);

            var alivePlayers = manager.Players.Where(p => !p.IsDead).ToList();
            if (alivePlayers.Count == 0)
            {
                manager.NextTurn();
                yield break;
            }

            // Mặc định tạo kỹ năng tấn công cơ bản
            SkillData skillToUse = ScriptableObject.CreateInstance<SkillData>();
            skillToUse.SkillID = "enemy_attack";
            skillToUse.SkillName = "Tấn Công Thường";
            skillToUse.PrimaryEffect = SkillEffectType.Damage;
            skillToUse.PrimaryMultiplier = 1.0f;

            CombatEntity target = null;

            // Xử lý AI nâng cao cho Boss hoặc AI quái thường
            bool isBoss = activeEntity.entityName.Contains("Boss") || activeEntity.maxHP >= 5000;

            if (isBoss)
            {
                float hpPercent = (float)activeEntity.currentHP / activeEntity.maxHP;
                
                // 1. Nếu HP dưới 30% và có cơ hội, Boss tự hồi máu
                if (hpPercent < 0.3f && activeEntity.currentMP >= 50 && Random.value < 0.6f)
                {
                    skillToUse.SkillID = "boss_heal";
                    skillToUse.SkillName = "Bá Thể Trị Liệu";
                    skillToUse.PrimaryEffect = SkillEffectType.Heal;
                    skillToUse.PrimaryMultiplier = 2.0f;
                    activeEntity.ConsumeMP(50);
                    target = activeEntity; // Tự trị liệu
                    Debug.Log($"[AI Boss] Sử dụng {skillToUse.SkillName} tự trị liệu!");
                }
                // 2. Sử dụng siêu kỹ năng càn quét (Heavy Attack) nếu có đủ MP
                else if (activeEntity.currentMP >= 80)
                {
                    skillToUse.SkillID = "boss_heavy_attack";
                    skillToUse.SkillName = "Thiên崩ĐịaLiệt (AOE)";
                    skillToUse.PrimaryEffect = SkillEffectType.Damage;
                    skillToUse.PrimaryMultiplier = 2.5f;
                    activeEntity.ConsumeMP(80);
                    
                    // AOE target: Tấn công tướng có HP thấp nhất hoặc ngẫu nhiên
                    target = alivePlayers.OrderBy(p => p.currentHP).First();
                    Debug.Log($"[AI Boss] Sử dụng tuyệt kỹ {skillToUse.SkillName} lên {target.entityName}!");
                }
                else
                {
                    // Đòn đánh thường nhưng nhắm vào kẻ địch có HP thấp nhất (kết liễu)
                    target = alivePlayers.OrderBy(p => p.currentHP).First();
                    activeEntity.AddMP(20); // Tích lũy MP khi đánh thường
                }
            }
            else
            {
                // Quái thường: Đánh ngẫu nhiên hoặc nhắm vào tướng phe ta có giáp yếu
                if (Random.value < 0.5f)
                {
                    target = alivePlayers.OrderBy(p => p.defense).First(); // Nhắm tướng thủ yếu nhất
                }
                else
                {
                    target = alivePlayers[Random.Range(0, alivePlayers.Count)];
                }
                activeEntity.AddMP(15);
            }

            if (target == null)
            {
                target = alivePlayers[Random.Range(0, alivePlayers.Count)];
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

