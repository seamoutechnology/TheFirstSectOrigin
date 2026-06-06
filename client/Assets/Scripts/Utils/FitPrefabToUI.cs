using UnityEngine;
using UnityEngine.EventSystems;

namespace GameClient.Utils
{
    public enum AspectMode
    {
        Fill,    // Phủ kín, không hở viền, có thể bị cắt ảnh (Crop)
        Fit,     // Hiện hết ảnh, có thể hở viền đen (Letterbox)
        Stretch  // Ép ảnh vừa khít, không hở viền, ảnh bị méo (Distort)
    }

    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class FitPrefabToUI : UIBehaviour
    {
        public AspectMode mode = AspectMode.Fill;
        [Header("Layer nền to nhất")]
        public SpriteRenderer referenceSprite;

        private RectTransform _rectTransform;

        protected override void Awake()
        {
            base.Awake();
            _rectTransform = GetComponent<RectTransform>();
        }

        protected override void OnRectTransformDimensionsChange()
        {
            base.OnRectTransformDimensionsChange();
            UpdateScale();
        }

        private bool _isUpdating = false;

        public void UpdateScale()
        {
            if (_isUpdating) return;
            if (referenceSprite == null || referenceSprite.sprite == null || _rectTransform == null) return;

            _isUpdating = true;
            try
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                if (canvas == null) return;
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                
                float canvasW = canvasRect.rect.width;
                float canvasH = canvasRect.rect.height;

                float spriteW = referenceSprite.sprite.rect.width;
                float spriteH = referenceSprite.sprite.rect.height;

                float scaleX = canvasW / spriteW;
                float scaleY = canvasH / spriteH;

                float finalScaleX = scaleX;
                float finalScaleY = scaleY;

                switch (mode)
                {
                    case AspectMode.Fill:
                        float fillScale = Mathf.Max(scaleX, scaleY);
                        finalScaleX = finalScaleY = fillScale;
                        break;
                    case AspectMode.Fit:
                        float fitScale = Mathf.Min(scaleX, scaleY);
                        finalScaleX = finalScaleY = fitScale;
                        break;
                    case AspectMode.Stretch:
                        break;
                }

                float multiplier = referenceSprite.sprite.pixelsPerUnit;
                
                Vector3 newScale = new Vector3(finalScaleX * multiplier, finalScaleY * multiplier, 1);
                if (transform.localScale != newScale)
                {
                    transform.localScale = newScale;
                }
                
                if (_rectTransform.anchoredPosition != Vector2.zero)
                {
                    _rectTransform.anchoredPosition = Vector2.zero;
                }
            }
            finally
            {
                _isUpdating = false;
            }
        }

        #if UNITY_EDITOR
        void Update() { if (!Application.isPlaying) UpdateScale(); }
        #endif
    }
}
