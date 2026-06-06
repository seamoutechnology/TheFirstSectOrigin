using System.Collections;
using UnityEngine;
using GameClient.Gameplay.Combat.Skills;

namespace GameClient.Gameplay.Combat.States
{
    public class ActionExecutionState : ICombatState
    {
        public IEnumerator Enter(CombatManager manager)
        {
            var skill = manager.SelectedSkill;
            var caster = manager.CurrentActiveEntity;
            var target = manager.SelectedTarget;

            if (skill != null && caster != null)
            {
                Debug.Log($"[Combat] {caster.entityName} sử dụng {skill.SkillName}!");
                
                if (skill.MPCost > 0)
                {
                    caster.ConsumeMP(skill.MPCost);
                }

                yield return manager.StartCoroutine(GenericSkillExecutor.Execute(skill, caster, target, (log) => {
                    manager.CombatLogs.Add(log);
                }));
            }
            else
            {
                Debug.LogWarning("[Combat] ActionExecutionState thiếu Skill hoặc Caster!");
            }

            yield return new WaitForSeconds(0.5f);

            manager.NextTurn();
        }

        public void Execute(CombatManager manager)
        {
        }

        public IEnumerator Exit(CombatManager manager)
        {
            manager.SelectedSkill = null;
            manager.SelectedTarget = null;
            yield break;
        }
    }
}
