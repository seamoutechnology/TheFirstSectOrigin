using System.Collections;
using System.Linq;
using UnityEngine;
using GameClient.Network.Pb; // Proto buffer namespace

namespace GameClient.Gameplay.Combat.Skills
{
    public class GenericSkillExecutor
    {
        public static IEnumerator Execute(SkillData data, CombatEntity caster, CombatEntity target, System.Action<CombatActionLog> onLogGenerated)
        {
            Debug.Log($"<color=cyan>[Combat]</color> {caster.entityName} thi triển {data.SkillName}!");

            if (data.IsAoE)
            {
                var targets = data.IsSupport 
                    ? (caster.isPlayer ? CombatManager.Instance.Players : CombatManager.Instance.Enemies) 
                    : (caster.isPlayer ? CombatManager.Instance.Enemies : CombatManager.Instance.Players);
                
                targets = targets.Where(e => !e.IsDead).ToList();

                foreach (var t in targets)
                {
                    ApplyEffect(data, caster, t, onLogGenerated);
                }
            }
            else
            {
                if (target == null && data.IsSupport) target = caster; // Default to self
                ApplyEffect(data, caster, target, onLogGenerated);
            }

            yield return new WaitForSeconds(0.5f);
        }

        private static void ApplyEffect(SkillData data, CombatEntity caster, CombatEntity target, System.Action<CombatActionLog> onLogGenerated)
        {
            if (target == null || target.IsDead) return;

            int damageDealt = 0;
            int hpHealed = 0;
            bool isCrit = false;

            ProcessSingleEffect(data.PrimaryEffect, data.PrimaryMultiplier, data.PrimaryFlatBonus, caster, target, ref damageDealt, ref hpHealed, ref isCrit);
            ProcessSingleEffect(data.SecondaryEffect, data.SecondaryMultiplier, data.SecondaryFlatBonus, caster, target, ref damageDealt, ref hpHealed, ref isCrit);

            if (damageDealt > 0)
            {
                target.TakeDamage(damageDealt, isCrit);
            }

            if (hpHealed > 0)
            {
                target.Heal(hpHealed);
            }

            onLogGenerated?.Invoke(new CombatActionLog
            {
                CasterId = caster.entityName,
                TargetId = target.entityName,
                SkillId = data.SkillID,
                Damage = damageDealt,
                Heal = hpHealed,
                IsCrit = isCrit
            });
        }

        private static void ProcessSingleEffect(SkillEffectType effect, float multiplier, int flatBonus, CombatEntity caster, CombatEntity target, ref int damageDealt, ref int hpHealed, ref bool isCrit)
        {
            if (multiplier == 0 && flatBonus == 0) return;

            switch (effect)
            {
                case SkillEffectType.Damage:
                    int baseDmg = (int)(caster.attack * multiplier) + flatBonus;
                    isCrit = Random.value > 0.8f;
                    if (isCrit) baseDmg = (int)(baseDmg * 1.5f);
                    damageDealt += baseDmg;
                    break;
                case SkillEffectType.Heal:
                    hpHealed += (int)(caster.attack * multiplier) + flatBonus;
                    break;
                case SkillEffectType.BuffAttack:
                    target.attack += flatBonus;
                    Debug.Log($"<color=yellow>[Buff]</color> {target.entityName} tăng {flatBonus} Tấn công.");
                    break;
                case SkillEffectType.BuffDefense:
                    target.defense += flatBonus;
                    Debug.Log($"<color=yellow>[Buff]</color> {target.entityName} tăng {flatBonus} Phòng thủ.");
                    break;
                case SkillEffectType.BuffSpeed:
                    target.speed += flatBonus;
                    Debug.Log($"<color=yellow>[Buff]</color> {target.entityName} tăng {flatBonus} Tốc độ.");
                    break;
                case SkillEffectType.RestoreMP:
                    target.AddMP(flatBonus);
                    Debug.Log($"<color=blue>[Buff]</color> {target.entityName} hồi {flatBonus} MP.");
                    break;
            }
        }
    }
}
