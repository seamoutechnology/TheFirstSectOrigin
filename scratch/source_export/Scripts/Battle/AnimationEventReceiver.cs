using System;
using UnityEngine;

namespace GameClient.Battle
{
    public class AnimationEventReceiver : MonoBehaviour
    {
        public event Action OnAttackHit;      // Gọi khi vũ khí chạm mục tiêu
        public event Action OnEffectTrigger;  // Gọi khi cần sinh ra VFX
        public event Action OnComboSignal;    // Gọi để kích hoạt tướng đồng đội (Combo)

        
        public void AE_OnHit()
        {
            OnAttackHit?.Invoke();
        }

        public void AE_OnEffect()
        {
            OnEffectTrigger?.Invoke();
        }

        public void AE_OnCombo()
        {
            OnComboSignal?.Invoke();
        }
    }
}
