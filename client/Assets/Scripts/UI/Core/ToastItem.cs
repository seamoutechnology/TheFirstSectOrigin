using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameClient.UI.Core
{
    [RequireComponent(typeof(CanvasGroup))]
    public class ToastItem : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Text messageText;
        private CanvasGroup canvasGroup;
        private RectTransform rectTransform;

        private string poolKey;
        private bool isBigToast;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            rectTransform = GetComponent<RectTransform>();
        }

        public void Setup(string message, float duration, string poolKeyAssigned, bool bigToast = false)
        {
            if (messageText != null)
            {
                messageText.text = message;
                messageText.textWrappingMode = TextWrappingModes.Normal; // Đảm bảo tự động xuống dòng trên TextMeshPro
            }

            poolKey = poolKeyAssigned;
            isBigToast = bigToast;

            canvasGroup.alpha = 0f;
            if (isBigToast)
            {
                rectTransform.localScale = Vector3.one * 0.8f;
            }

            StopAllCoroutines();
            StartCoroutine(ToastRoutine(duration));
        }

        private IEnumerator ToastRoutine(float duration)
        {
            float timer = 0f;
            float animTime = 0.3f;

            while (timer < animTime)
            {
                timer += Time.deltaTime;
                float t = timer / animTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

                if (isBigToast)
                {
                    rectTransform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, t);
                }
                yield return null;
            }

            canvasGroup.alpha = 1f;

            yield return new WaitForSeconds(duration);

            timer = 0f;
            while (timer < animTime)
            {
                timer += Time.deltaTime;
                float t = timer / animTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }

            ReleaseOrDestroy();
        }

        public void DismissImmediate()
        {
            StopAllCoroutines();
            ReleaseOrDestroy();
        }

        private void ReleaseOrDestroy()
        {
            if (!string.IsNullOrEmpty(poolKey))
            {
                GameClient.Managers.ObjectPoolManager.Instance.Release(poolKey, this.gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
