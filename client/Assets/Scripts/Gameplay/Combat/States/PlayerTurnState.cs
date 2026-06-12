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

            // Quyết định kỹ năng dựa trên lượng HP và MP của nhân vật
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
