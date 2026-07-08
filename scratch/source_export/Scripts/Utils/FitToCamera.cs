using UnityEngine;

namespace GameClient.Utils
{
    public class FitToCamera : MonoBehaviour
    {
        public SpriteRenderer targetSprite;

        public Camera targetCamera;

        public bool snapToCameraCenter = false;

        public float marginBuffer = 1.02f;

        void Awake()
        {
        }

        void Start()
        {
            Fit();
        }

        void LateUpdate()
        {
            Fit();
        }

        public void Fit()
        {
            if (targetSprite == null || targetSprite.sprite == null)
            {
                Debug.LogError("[FitToCamera] LỖI: Bạn chưa kéo thả Layer Background vào ô Target Sprite, hoặc Layer đó không có ảnh!");
                return;
            }

            float originalWidth = targetSprite.sprite.bounds.size.x;
            float originalHeight = targetSprite.sprite.bounds.size.y;

            if (originalWidth <= 0 || originalHeight <= 0) return;

            float containerWidth = 0f;
            float containerHeight = 0f;

            Canvas parentCanvas = GetComponentInParent<Canvas>();
            
            if (parentCanvas != null)
            {
                RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
                containerWidth = canvasRect.rect.width;
                containerHeight = canvasRect.rect.height;
            }
            else
            {
                Camera cam = targetCamera != null ? targetCamera : Camera.main;
                if (cam == null) return;

                if (snapToCameraCenter)
                {
                    transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, transform.position.z);
                }

                float screenAspect = (float)Screen.width / Screen.height;
                containerHeight = cam.orthographicSize * 2.0f;
                containerWidth = containerHeight * screenAspect;
            }

            float scaleX = containerWidth / originalWidth;
            float scaleY = containerHeight / originalHeight;

            if (marginBuffer < 1.0f) marginBuffer = 1.02f;

            float maxScale = Mathf.Max(scaleX, scaleY) * marginBuffer;
            
            transform.localScale = new Vector3(maxScale, maxScale, 1f);
        }
    }
}
