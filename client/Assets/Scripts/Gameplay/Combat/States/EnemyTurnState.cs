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
            Debug.Log($"[Combat] Đến lượt kẻ địch: {manager.CurrentActiveEntity.entityName}");
            yield return new WaitForSeconds(1f);

            var alivePlayers = manager.Players.Where(p => !p.IsDead).ToList();
            if (alivePlayers.Count > 0)
            {
                CombatEntity target = alivePlayers[Random.Range(0, alivePlayers.Count)];
                
                SkillData attackSkill = ScriptableObject.CreateInstance<SkillData>(); 
                attackSkill.SkillID = "enemy_attack";
                attackSkill.SkillName = "Cắn Xé";
                attackSkill.PrimaryEffect = SkillEffectType.Damage;
                attackSkill.PrimaryMultiplier = 1.0f;
                
                manager.ExecuteAction(attackSkill, target);
            }
            else
            {
                manager.NextTurn();
            }
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
