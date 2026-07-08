using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using GameClient.Managers;

namespace GameClient.UI
{
    public class BattleHeroDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public long HeroId { get; private set; }
        public bool IsOnBoard { get; private set; }
        public int BoardPosition { get; private set; } = -1;

        private Canvas _canvas;
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Vector3 _startPosition;
        private Transform _originalParent;
        private BattlePrepPanel _panel;

        public void Setup(long heroId, bool isOnBoard, int boardPosition, BattlePrepPanel panel)
        {
            HeroId = heroId;
            IsOnBoard = isOnBoard;
            BoardPosition = boardPosition;
            _panel = panel;

            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();
            
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
            {
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_panel == null) return;

            // Rule: If dragging from owned list, but board already has 9 heroes, cancel drag
            if (!IsOnBoard && _panel.GetFormationCount() >= 9)
            {
                eventData.pointerDrag = null; // Cancel drag operation in Unity
                ToastManager.Instance.ShowBigToast(GameClient.Managers.LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.BATTLE_COMBAT, "combat_max_heroes_drag"));
                return;
            }

            _startPosition = transform.position;
            _originalParent = transform.parent;

            // Lưu lại kích thước và tỉ lệ gốc trước khi đổi cha để tránh bị bóp méo
            Vector2 originalSize = _rectTransform.sizeDelta;
            Vector3 originalScale = _rectTransform.localScale;

            // Di chuyển lên canvas root để vẽ đè lên trên tất cả mọi thứ
            transform.SetParent(_canvas.transform, true);

            // Căn lề giữa và reset pivot để tránh bị lệch vị trí dưới con trỏ chuột khi kéo
            _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _rectTransform.sizeDelta = originalSize;
            _rectTransform.localScale = originalScale;

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0.7f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 localPoint;
            Camera cam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, eventData.position, cam, out localPoint))
            {
                _rectTransform.anchoredPosition = localPoint;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;

            // Check if dropped on a Slot or somewhere else
            GameObject dropTarget = eventData.pointerCurrentRaycast.gameObject;
            BattleFormationSlot slot = null;

            if (dropTarget != null)
            {
                slot = dropTarget.GetComponent<BattleFormationSlot>();
                if (slot == null)
                {
                    slot = dropTarget.GetComponentInParent<BattleFormationSlot>();
                }
            }

            if (slot != null)
            {
                // Dropped on a slot! Handle assignment/swap
                _panel.HandleHeroDroppedOnSlot(HeroId, slot.SlotIndex, IsOnBoard, BoardPosition);
                
                if (IsOnBoard)
                {
                    // Nếu là ô trên bàn cờ, không được huỷ ô! Trả nó về cha cũ, phục hồi thứ tự phân cấp (Sibling Index) và reset vị trí.
                    transform.SetParent(_originalParent, false);
                    transform.SetSiblingIndex(BoardPosition);
                    _rectTransform.anchoredPosition = Vector2.zero;
                }
                else
                {
                    Destroy(gameObject); // Thẻ tướng ở danh sách dưới kéo lên thì huỷ được vì sẽ load lại
                }
            }
            else
            {
                // Dropped outside a slot
                if (IsOnBoard)
                {
                    // Dragged from board to outside/down -> Remove hero from formation
                    _panel.RemoveHeroFromFormation(HeroId);
                    
                    // Trả ô đấu về lại vị trí cũ trên bàn cờ, phục hồi Sibling Index để giữ nguyên bố cục lưới
                    transform.SetParent(_originalParent, false);
                    transform.SetSiblingIndex(BoardPosition);
                    _rectTransform.anchoredPosition = Vector2.zero;
                }
                else
                {
                    // Dragged from owned list to outside -> Snap back
                    transform.SetParent(_originalParent, false);
                    transform.position = _startPosition;
                }
            }
        }
    }
}
