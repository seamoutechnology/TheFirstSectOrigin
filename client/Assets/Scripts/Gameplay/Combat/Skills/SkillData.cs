using UnityEngine;

namespace GameClient.Gameplay.Combat.Skills
{
    public enum SkillEffectType
    {
        Damage,
        Heal,
        BuffAttack,
        BuffDefense,
        BuffSpeed,
        RestoreMP
    }

    [CreateAssetMenu(fileName = "NewSkill", menuName = "TheFirstSectOrigin/SkillData")]
    public class SkillData : ScriptableObject
    {
        [Header("Basic Info")]
        public string SkillID;
        public string SkillName;
        public string Description;
        public int MPCost;

        [Header("Targeting")]
        public bool IsAoE;
        public bool IsSupport; // True means it targets self/allies

        [Header("Effect 1")]
        public SkillEffectType PrimaryEffect;
        public float PrimaryMultiplier = 1.0f; // e.g. Damage = Attack * 1.5
        public int PrimaryFlatBonus = 0; // e.g. Heal +50 flat

        [Header("Effect 2 (Optional)")]
        public SkillEffectType SecondaryEffect;
        public float SecondaryMultiplier = 0.0f;
        public int SecondaryFlatBonus = 0;

        [Header("VFX")]
        public GameObject CastVFX;
        public GameObject ImpactVFX;
    }
}
