using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace GameClient.Managers
{
    public class TutorialGuideOverlay : MonoBehaviour, IPointerClickHandler
    {
        private RectTransform _targetRect;
        private RectTransform _pointerVisual;
        private string _actionToTrigger;

        public void Setup(TutorialTarget target, string actionToTrigger)
        {
            _targetRect = target.GetComponent<RectTransform>();
            _actionToTrigger = actionToTrigger;

            // Thiết lập Canvas cho overlay che phủ toàn màn hình
            var rect = gameObject.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // Nền tối mờ nhẹ chặn click
            var bgImage = gameObject.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.4f); // 40% tối
            bgImage.raycastTarget = true;

            // Tạo Pointer (mũi tên hoặc bàn tay hướng dẫn)
            var pointerGo = new GameObject("PointerGuide");
            pointerGo.transform.SetParent(transform, false);
            _pointerVisual = pointerGo.AddComponent<RectTransform>();
            _pointerVisual.sizeDelta = new Vector2(64, 64);

            var pointerImage = pointerGo.AddComponent<Image>();
            // Fallback load mũi tên
            pointerImage.sprite = Resources.Load<Sprite>("Sprites/Menu/Btn_Click_Highlight_01");
            if (pointerImage.sprite == null)
            {
                // Nếu không tìm thấy sprite nào, sử dụng một sprite trắng cơ bản
                Texture2D tex = new Texture2D(1, 1);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                pointerImage.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
            }
            pointerImage.color = Color.yellow;

            AlignAndAnimate();
        }

        private void Update()
        {
            if (_targetRect == null)
            {
                Destroy(gameObject);
                return;
            }
            FollowTarget();
        }

        private void FollowTarget()
        {
            if (_targetRect == null || _pointerVisual == null) return;
            Vector3 worldPos = _targetRect.position;
            // Di chuyển pointer nằm phía trên Target một chút
            _pointerVisual.position = worldPos + new Vector3(0, 60f, 0);
        }

        private void AlignAndAnimate()
        {
            FollowTarget();

            // Hiệu ứng nẩy (bouncing)
            _pointerVisual.transform.localScale = Vector3.one;
            _pointerVisual.transform.DOLocalMoveY(_pointerVisual.transform.localPosition.y - 20f, 0.5f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutQuad);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_targetRect == null) return;

            // Kiểm tra xem vị trí chạm có nằm bên trong Rect của target không
            if (RectTransformUtility.RectangleContainsScreenPoint(_targetRect, eventData.position, eventData.pressEventCamera))
            {
                Debug.Log($"[TutorialGuideOverlay] Click chính xác vào target: {_targetRect.name}");
                
                // Thực thi hành động tương tác
                var button = _targetRect.GetComponent<Button>();
                if (button != null && button.interactable)
                {
                    button.onClick.Invoke();
                }

                // Báo cho TutorialManager biết hành động đã được thực hiện
                if (TutorialManager.Instance != null && !string.IsNullOrEmpty(_actionToTrigger))
                {
                    TutorialManager.Instance.TriggerAction(_actionToTrigger);
                }

                Destroy(gameObject);
            }
            else
            {
                // Click sai vị trí -> Rung lắc cảnh báo
                _pointerVisual.DOShakePosition(0.3f, 10f);
            }
        }
    }
}
