using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using DG.Tweening;

namespace GameClient.UI
{
    public class DraggableActionButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler
    {
        [Header("Settings")]
        public RectTransform actionZone; 
        public float activationDistance = 100f; 
        public float snapPadding = 20f; 
        public float snapSpeed = 15f;   
        public float idleOpacity = 0.6f; // Độ mờ khi rảnh (60%)
        
        [Header("Events")]
        public UnityEngine.Events.UnityEvent onClick; 
        public UnityEngine.Events.UnityEvent onDismiss; 

        private CanvasGroup _zoneCanvasGroup;
        private CanvasGroup _iconCanvasGroup;
        private RectTransform _rectTransform;
        private Canvas _canvas;
        private bool _isDragging = false;
        
        [HideInInspector] 
        public bool KeepFullyOpaque = false;
        
        private bool _wasDraggingRecently = false; 
        private float _pointerDownTime; 
        
        private int _lastScreenWidth;
        private int _lastScreenHeight;

        void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvas = GetComponentInParent<Canvas>();

            _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _rectTransform.pivot = new Vector2(0.5f, 0.5f);

            _iconCanvasGroup = GetComponent<CanvasGroup>();
            if (_iconCanvasGroup == null) _iconCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            _iconCanvasGroup.alpha = idleOpacity;

            if (actionZone != null)
            {
                _zoneCanvasGroup = actionZone.GetComponent<CanvasGroup>();
                if (_zoneCanvasGroup == null) _zoneCanvasGroup = actionZone.gameObject.AddComponent<CanvasGroup>();
                _zoneCanvasGroup.alpha = 0;
                actionZone.gameObject.SetActive(false);
            }
        }

        void Start()
        {
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            SnapToEdge();
        }

        void Update()
        {
            if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
            {
                _lastScreenWidth = Screen.width;
                _lastScreenHeight = Screen.height;
                SnapToEdge();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pointerDownTime = Time.time;
            _isDragging = false;
            _wasDraggingRecently = false;
            
            _iconCanvasGroup.DOFade(1f, 0.1f);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_isDragging && !_wasDraggingRecently && (Time.time - _pointerDownTime) < 0.3f)
            {
                onClick?.Invoke();
            }
            
            if (!_isDragging && !KeepFullyOpaque)
                _iconCanvasGroup.DOFade(idleOpacity, 0.2f);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _isDragging = true;
            _wasDraggingRecently = true;
            _rectTransform.DOKill();
            _iconCanvasGroup.DOKill();
            if (_zoneCanvasGroup != null) _zoneCanvasGroup.DOKill();
            
            _iconCanvasGroup.alpha = 1f;

            if (actionZone != null)
            {
                actionZone.gameObject.SetActive(true);
                _zoneCanvasGroup.DOFade(0.45f, 0.15f);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 localPoint;
            Camera cam = (_canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : _canvas.worldCamera;
            
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas.transform as RectTransform, eventData.position, cam, out localPoint))
            {
                RectTransform canvasRect = _canvas.transform as RectTransform;
                float halfW = canvasRect.rect.width / 2f;
                float halfH = canvasRect.rect.height / 2f;
                float iconHalfW = _rectTransform.rect.width / 2f;
                float iconHalfH = _rectTransform.rect.height / 2f;

                localPoint.x = Mathf.Clamp(localPoint.x, -halfW + iconHalfW, halfW - iconHalfW);
                localPoint.y = Mathf.Clamp(localPoint.y, -halfH + iconHalfH, halfH - iconHalfH);

                _rectTransform.anchoredPosition = localPoint;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _isDragging = false;
            
            float dist = Vector2.Distance(_rectTransform.position, actionZone.position);
            if (dist < activationDistance)
            {
                onDismiss?.Invoke();
                _iconCanvasGroup.DOFade(0f, 0.2f).OnComplete(() => gameObject.SetActive(false));
            }
            else
            {
                _iconCanvasGroup.DOFade(idleOpacity, 0.3f);
                SnapToEdge();
            }

            if (actionZone != null)
                _zoneCanvasGroup.DOFade(0f, 0.2f).OnComplete(() => actionZone.gameObject.SetActive(false));

            DOVirtual.DelayedCall(0.1f, () => _wasDraggingRecently = false);
        }

        private void SnapToEdge()
        {
            if (_isDragging) return;

            RectTransform canvasRect = _canvas.transform as RectTransform;
            float canvasW = canvasRect.rect.width;
            float canvasH = canvasRect.rect.height;

            float iconW = _rectTransform.rect.width;
            float iconH = _rectTransform.rect.height;

            float leftX = -canvasW / 2 + iconW / 2 + snapPadding;
            float rightX = canvasW / 2 - iconW / 2 - snapPadding;
            float topY = canvasH / 2 - iconH / 2 - snapPadding;
            float bottomY = -canvasH / 2 + iconH / 2 + snapPadding;

            Vector2 currentPos = _rectTransform.anchoredPosition;
            Vector2 targetPos = currentPos;

            float dLeft = Mathf.Abs(currentPos.x - leftX);
            float dRight = Mathf.Abs(currentPos.x - rightX);
            float dTop = Mathf.Abs(currentPos.y - topY);
            float dBottom = Mathf.Abs(currentPos.y - bottomY);

            float min = Mathf.Min(dLeft, dRight, dTop, dBottom);

            if (min == dLeft) targetPos.x = leftX;
            else if (min == dRight) targetPos.x = rightX;
            else if (min == dTop) targetPos.y = topY;
            else if (min == dBottom) targetPos.y = bottomY;

            _rectTransform.DOAnchorPos(targetPos, 1f / snapSpeed).SetEase(Ease.OutBack);
        }
    }
}
