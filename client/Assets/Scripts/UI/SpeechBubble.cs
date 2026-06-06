using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;
using GameClient.Managers;

namespace GameClient.UI
{
    public class SpeechBubble : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text txtDialogue;
        [SerializeField] private RectTransform bgBubbleRect;

        [Header("Animation Settings")]
        [SerializeField] private float popupDuration = 0.3f;
        [SerializeField] private float fadeDuration = 0.2f;
        [SerializeField] private float targetScale = 0.8f;      // Tỷ lệ co dãn của bong bóng thoại (nhỏ hơn 1 để tránh quá to)

        [Header("Auto Duration Settings (Fallback)")]
        [SerializeField] private bool useAutoDuration = true;
        [SerializeField] private float minDuration = 1.8f;              // Cho câu thoại ngắn
        [SerializeField] private float maxDuration = 8.0f;              // Cho câu thoại dài
        [SerializeField] private float charactersPerSecond = 15f;       // Tốc độ đọc ước tính nếu không có voice

        private Transform _targetCharacter;
        private Vector3 _offset = new Vector3(0, 2.2f, 0); // Khoảng cách offset trên đầu nhân vật

        public void Setup(Transform target, string text, AudioClip voiceClip = null, float fallbackDuration = -1f)
        {
            _targetCharacter = target;
            if (txtDialogue != null)
            {
                txtDialogue.text = text;
            }

            if (bgBubbleRect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(bgBubbleRect);
            }

            UpdatePosition();

            gameObject.SetActive(true);

            float finalDuration = fallbackDuration;

            if (voiceClip != null && AudioManager.Instance != null)
            {
                finalDuration = voiceClip.length;
                
                AudioManager.Instance.PlayExclusiveSFX(voiceClip);
                Debug.Log($"[SpeechBubble] Phát Voice qua AudioManager dài: {finalDuration:F2} giây");
            }
            else if (finalDuration < 0f && useAutoDuration)
            {
                finalDuration = Mathf.Clamp(text.Length / charactersPerSecond, minDuration, maxDuration);
            }

            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one * targetScale, popupDuration)
                .SetEase(Ease.OutBack);

            Invoke(nameof(HideAndDestroy), finalDuration);
        }

        private void Update()
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            if (_targetCharacter == null || Camera.main == null) return;

            Vector3 worldTargetPos = _targetCharacter.position + _offset;
            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldTargetPos);

            transform.position = screenPos;
        }

        private void HideAndDestroy()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.StopAllSFX();
            }

            transform.DOScale(Vector3.zero, fadeDuration)
                .SetEase(Ease.InBack)
                .OnComplete(() => Destroy(gameObject));
        }
    }
}
