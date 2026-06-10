using UnityEngine;
using DG.Tweening; // Sử dụng DOTween để trượt Camera mượt mà
using GameClient.Gameplay.BaseBuilder;

namespace GameClient.BaseBuilding.Core
{
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        [Header("Pan Settings")]
        public float panSpeed = 20f;
        public float zoomSpeed = 5f;
        public float minZoom = 3f;
        public float maxZoom = 15f;

        [Header("Bounds")]
        public Vector2 minBounds = new Vector2(-50, -50);
        public Vector2 maxBounds = new Vector2(50, 50);

        private Camera _cam;
        private Vector3 _dragOrigin;
        private bool _isDragging = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _cam = GetComponent<Camera>();
            if (_cam == null) _cam = Camera.main;
        }

        private void Start()
        {
            UpdateCameraBounds();
        }

        public void UpdateCameraBounds()
        {
            if (BaseGridManager.Instance != null)
            {
                minBounds = new Vector2(-2f, -2f);
                maxBounds = new Vector2(BaseGridManager.Instance.Width + 2f, BaseGridManager.Instance.Height + 2f);
            }
        }

        private void Update()
        {
            HandleMousePan();
            HandleZoom();
        }

        private void LateUpdate()
        {
            ClampCameraPosition();
        }

        private void ClampCameraPosition()
        {
            if (_cam == null) return;
            Vector3 pos = _cam.transform.position;
            pos.x = Mathf.Clamp(pos.x, minBounds.x, maxBounds.x);
            pos.y = Mathf.Clamp(pos.y, minBounds.y, maxBounds.y);
            _cam.transform.position = pos;
        }

        private Vector3 _lastMousePos;

        private void HandleMousePan()
        {
            if (_cam == null) return;

            var inputManager = GameClient.Managers.InputManager.Instance;
            if (inputManager == null) return;

            // Chặn kéo camera khi đang kéo toà nhà trên Mobile
            if (BuildingController.Instance != null && 
                BuildingController.Instance.IsPlacing && 
                BuildingController.Instance.IsPointerDownInsidePreview)
            {
                _isDragging = false;
                return;
            }

            Vector2 pointerPos = inputManager.GetPointerPosition();
            bool isDown = inputManager.IsPointerPanDown();
            bool isPressed = inputManager.IsPointerPanPressed();

            if (isDown)
            {
                _lastMousePos = new Vector3(pointerPos.x, pointerPos.y, -_cam.transform.position.z);
                _isDragging = true;
            }
            else if (isPressed && _isDragging)
            {
                Vector3 currentPos = new Vector3(pointerPos.x, pointerPos.y, -_cam.transform.position.z);
                Vector3 delta = _cam.ScreenToWorldPoint(_lastMousePos) - _cam.ScreenToWorldPoint(currentPos);
                
                if (delta.sqrMagnitude > 0.000001f)
                {
                    _cam.transform.position += delta;
                }
                _lastMousePos = currentPos;
            }
            else if (!isPressed && _isDragging)
            {
                _isDragging = false;
            }
        }

        private void HandleZoom()
        {
            if (_cam == null) return;
            float scrollAmount = 0f;

            if (GameClient.Managers.InputManager.Instance != null)
            {
                float imScroll = GameClient.Managers.InputManager.Instance.GetZoomDelta();
                if (Mathf.Abs(imScroll) > 0.001f)
                {
                    scrollAmount = imScroll;
                }
            }
            else 
            {
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse != null)
                {
                    scrollAmount = mouse.scroll.ReadValue().y * 0.01f;
                }
            }

            if (Mathf.Abs(scrollAmount) > 0.0001f)
            {
                _cam.orthographicSize -= scrollAmount * zoomSpeed;
                _cam.orthographicSize = Mathf.Clamp(_cam.orthographicSize, minZoom, maxZoom);
            }
        }

        public void FocusTo(Vector2 targetPosition, float duration = 1f)
        {
            Vector3 targetPos3D = new Vector3(targetPosition.x, targetPosition.y, _cam.transform.position.z);
            _cam.transform.DOMove(targetPos3D, duration).SetEase(Ease.OutCubic);
        }

        public void FocusTo(Vector2 targetPosition, float targetZoom, float duration = 1f)
        {
            Vector3 targetPos3D = new Vector3(targetPosition.x, targetPosition.y, _cam.transform.position.z);
            _cam.transform.DOMove(targetPos3D, duration).SetEase(Ease.OutCubic);
            _cam.DOOrthoSize(targetZoom, duration).SetEase(Ease.OutCubic);
        }
    }
}
