using UnityEngine;
using UnityEngine.U2D.Animation;

namespace GameClient.Battle
{
    public class HeroVisual : MonoBehaviour
    {
        [Header("References")]
        public Animator animator;
        public SpriteLibrary spriteLibrary; // Dùng để thay đổi trang phục (Skin)
        
        public void ChangeSkin(string skinLabel)
        {
            if (spriteLibrary != null)
            {
                Debug.Log($"[HeroVisual] Đã đổi trang phục sang: {skinLabel}");
            }
        }

        public void PlayAttackEffect()
        {
        }
    }
}
