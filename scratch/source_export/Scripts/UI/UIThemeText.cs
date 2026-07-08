using TMPro;
using UnityEngine;

namespace GameClient.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    [ExecuteAlways]
    public class UIThemeText : MonoBehaviour
    {
        [Header("Cấu hình Theme")]
        [SerializeField] private UIThemeData themeData;
        [SerializeField] private UIStyleType styleType;

        private TextMeshProUGUI _textComponent;

        private void Awake()
        {
            _textComponent = GetComponent<TextMeshProUGUI>();
            ApplyStyle();
        }

        private void Start()
        {
            ApplyStyle();
        }

        public void ApplyStyle()
        {
            if (_textComponent == null)
                _textComponent = GetComponent<TextMeshProUGUI>();

            if (themeData == null) return;

            if (themeData.TryGetStyle(styleType, out UIStyleConfig config))
            {
                if (config.fontAsset != null)
                    _textComponent.font = config.fontAsset;

                if (config.fontSize > 0)
                {
                    _textComponent.fontSize = config.fontSize;
                    _textComponent.overflowMode = TextOverflowModes.Overflow;
                }

                // Chỉ áp dụng màu nếu màu đó có độ hiển thị (Alpha > 0) hoặc không phải hoàn toàn đen trong suốt
                if (config.textColor.a > 0f)
                    _textComponent.color = config.textColor;

                // Chỉ truy cập và thay đổi Material khi đang chạy game (Play Mode) để tránh lỗi gán sai Material/SubMesh của TMPro trong Editor
                if (Application.isPlaying && (config.useOutline || config.faceDilate > 0f))
                {
                    _textComponent.extraPadding = true; // Tự động bật Extra Padding để tránh mất viền/cắt chữ
                    
                    if (_textComponent.fontSharedMaterial != null && _textComponent.fontMaterial != null)
                    {
                        if (config.useOutline)
                        {
                            _textComponent.fontMaterial.EnableKeyword("OUTLINE_ON");
                            _textComponent.fontMaterial.SetColor("_OutlineColor", config.outlineColor);
                            _textComponent.fontMaterial.SetFloat("_OutlineWidth", config.outlineWidth);
                        }
                        else
                        {
                            _textComponent.fontMaterial.DisableKeyword("OUTLINE_ON");
                            _textComponent.fontMaterial.SetFloat("_OutlineWidth", 0f);
                        }
                        _textComponent.fontMaterial.SetFloat("_FaceDilate", config.faceDilate);
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // Cập nhật trực quan Font, Cỡ chữ, và Màu sắc ngay trong Editor khi thay đổi thiết lập
            ApplyStyle();
        }
#endif
    }
}
