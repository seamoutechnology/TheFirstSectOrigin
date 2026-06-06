using UnityEngine;

namespace GameClient.Gameplay.BaseBuilder
{
    public class DummyWeaponAnimatorHandler : MonoBehaviour
    {
        [Header("Animator Components")]
        [SerializeField] private Animator animator;

        [Header("Weapon Animators (Override Controllers)")]
        [SerializeField] private AnimatorOverrideController swordAnimatorOverride;
        [SerializeField] private AnimatorOverrideController bowAnimatorOverride;
        [SerializeField] private AnimatorOverrideController staffAnimatorOverride;
        [SerializeField] private RuntimeAnimatorController unarmedAnimatorController; // Animator gốc ban đầu

        public void SwitchWeaponAnimation(string weaponType)
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
                if (animator == null) return;
            }

            switch (weaponType.ToLower())
            {
                case "sword":
                    if (swordAnimatorOverride != null)
                        animator.runtimeAnimatorController = swordAnimatorOverride;
                    break;
                case "bow":
                    if (bowAnimatorOverride != null)
                        animator.runtimeAnimatorController = bowAnimatorOverride;
                    break;
                case "staff":
                    if (staffAnimatorOverride != null)
                        animator.runtimeAnimatorController = staffAnimatorOverride;
                    break;
                case "unarmed":
                default:
                    if (unarmedAnimatorController != null)
                        animator.runtimeAnimatorController = unarmedAnimatorController;
                    break;
            }

            Debug.Log($"[DummyWeaponAnimatorHandler] Đã chuyển sang hoạt họa cho vũ khí: {weaponType}");
        }
    }
}
