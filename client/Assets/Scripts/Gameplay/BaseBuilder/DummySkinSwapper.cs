using UnityEngine;
#if UNITY_2D_ANIMATION
using UnityEngine.U2D.Animation;
#endif

namespace GameClient.Gameplay.BaseBuilder
{
    public class DummySkinSwapper : MonoBehaviour
    {
#if UNITY_2D_ANIMATION
        [Header("Sprite Resolvers")]
        [SerializeField] private SpriteResolver hairResolver;
        [SerializeField] private SpriteResolver bodyResolver;
        [SerializeField] private SpriteResolver weaponResolver;

        public void SwapSkinPart(string category, string label)
        {
            switch (category.ToLower())
            {
                case "hair":
                    if (hairResolver != null) SetResolver(hairResolver, "Hair", label);
                    break;
                case "body":
                    if (bodyResolver != null) SetResolver(bodyResolver, "Body", label);
                    break;
                case "weapon":
                    if (weaponResolver != null) SetResolver(weaponResolver, "Weapon", label);
                    break;
                default:
                    Debug.LogWarning($"[DummySkinSwapper] Không tìm thấy category: {category}");
                    break;
            }
        }

        private void SetResolver(SpriteResolver resolver, string category, string label)
        {
            resolver.SetCategoryAndLabel(category, label);
            resolver.ResolveAndUpdateSprite();
            Debug.Log($"[DummySkinSwapper] Đã đổi bộ phận '{category}' thành '{label}'");
        }
#else
        [Header("Chưa cài package Unity 2D Animation - Tạm dùng Mock Swap")]
        [SerializeField] private SpriteRenderer hairRenderer;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer weaponRenderer;

        public void SwapSkinPart(string category, string label)
        {
            Sprite newSprite = Resources.Load<Sprite>($"GameData/Skins/{category}/{label}");
            if (newSprite == null)
            {
                Debug.LogWarning($"[DummySkinSwapper] Không tìm thấy Sprite dự phòng tại GameData/Skins/{category}/{label}");
                return;
            }

            switch (category.ToLower())
            {
                case "hair":
                    if (hairRenderer != null) hairRenderer.sprite = newSprite;
                    break;
                case "body":
                    if (bodyRenderer != null) bodyRenderer.sprite = newSprite;
                    break;
                case "weapon":
                    if (weaponRenderer != null) weaponRenderer.sprite = newSprite;
                    break;
            }
        }
#endif
    }
}
