using UnityEngine;
using GameClient.BaseBuilding.Grid;
using GameClient.Gameplay.BaseBuilder;
using GameClient.Managers;
using System.Threading.Tasks;

namespace GameClient.BaseBuilding.Core
{
    public class BuildingController : MonoBehaviour
    {
        [Header("Preview Settings")]
        public SpriteRenderer previewRenderer;
        public Color validColor = new Color(0, 1, 0, 0.5f);
        public Color invalidColor = new Color(1, 0, 0, 0.5f);

        public static BuildingController Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public bool IsPlacing => _isPlacing;
        public bool IsPointerDownInsidePreview => _isPointerDownInsidePreview;

        private bool _isPlacing;
        private int _currentWidth = 2;
        private int _currentHeight = 2;
        private string _prefabAddressToPlace;
        private string _buildingIdToPlace;
        private long _instanceIdToPlace;
        private int _levelToPlace;
        private long _movingOriginalInstanceId;

        // Trạng thái phục vụ kéo thả & di chuyển nhà
        private bool _currentFlipX;
        private Vector2 _pointerDownPosition;
        private bool _isMovingExisting;
        private string _movingBuildingId;
        private int _movingOriginalX;
        private int _movingOriginalY;
        private int _movingOriginalLevel;
        private bool _movingOriginalFlipX;
        private bool _hasGhostLoaded;
        private bool _isDraggingPreview;
        private Vector2Int _previewGridPos;
        private bool _isPreviewAttachedToMouse;
        private bool _isPointerDownInsidePreview;

        // Trạng thái giữ chuột để mở Build Panel
        private float _pointerDownTime;
        private bool _hasTriggeredHold;
        private BuildingInstance _pressedBuilding;

        void Start()
        {
            GameClient.Managers.InputManager.Instance.RegisterTap("RotateBuilding", UnityEngine.InputSystem.Key.R, () => 
            {
                if (_isPlacing) RotateBuilding();
            });
        }

        void OnDestroy()
        {
            if (GameClient.Managers.InputManager.Instance != null)
            {
                GameClient.Managers.InputManager.Instance.Unregister("RotateBuilding");
            }
        }

        void Update()
        {
            if (GameClient.Managers.InputManager.Instance == null) return;
            var inputManager = GameClient.Managers.InputManager.Instance;
            Vector2 pointerPos = inputManager.GetPointerPosition();

            // Theo dõi vị trí Pointer Down ban đầu
            if (inputManager.IsPrimaryPointerDown())
            {
                _pointerDownPosition = pointerPos;
                _pointerDownTime = Time.time;
                _hasTriggeredHold = false;
                _pressedBuilding = null;

                if (!_isPlacing && Camera.main != null && UnityEngine.EventSystems.EventSystem.current != null)
                {
                    // Kiểm tra xem có click trúng UI không trước khi ghi nhận building
                    var pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                    pointerData.position = pointerPos;
                    var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                    UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, results);
                    
                    bool blockedByRealUI = false;
                    foreach (var result in results)
                    {
                        var parentCanvas = result.gameObject.GetComponentInParent<Canvas>();
                        if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
                            continue;

                        var selectable = result.gameObject.GetComponentInParent<UnityEngine.UI.Selectable>();
                        var scrollRect = result.gameObject.GetComponentInParent<UnityEngine.UI.ScrollRect>();
                        if ((selectable != null && selectable.interactable) || scrollRect != null)
                        {
                            blockedByRealUI = true;
                            break;
                        }

                        var canvasGroup = result.gameObject.GetComponentInParent<CanvasGroup>();
                        if (canvasGroup != null && canvasGroup.blocksRaycasts && canvasGroup.interactable &&
                            parentCanvas != null && parentCanvas.sortingOrder > 0)
                        {
                            blockedByRealUI = true;
                            break;
                        }
                    }

                    if (!blockedByRealUI)
                    {
                        Vector3 screenPos3D = new Vector3(pointerPos.x, pointerPos.y, Mathf.Abs(Camera.main.transform.position.z));
                        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos3D);
                        Vector2Int clickGridPos = BaseGridManager.Instance.WorldToGridPosition(worldPos);
                        _pressedBuilding = BaseBuildingManager.Instance.GetBuildingAt(clickGridPos.x, clickGridPos.y);
                    }
                }
            }

            if (!_isPlacing)
            {
                // Kiểm tra sự kiện Giữ (Hold) 1 giây
                if (inputManager.IsPrimaryPointerPressed() && _pressedBuilding != null && !_hasTriggeredHold)
                {
                    if (Time.time - _pointerDownTime >= 1.0f)
                    {
                        float dragDist = Vector2.Distance(_pointerDownPosition, pointerPos);
                        if (dragDist < 20f) // Tránh kích hoạt nhầm khi đang cuộn camera/kéo bản đồ
                        {
                            _hasTriggeredHold = true;
                            Debug.Log($"[BuildingController] Giữ toà nhà 1s -> Mở Build/Action Panel: {_pressedBuilding.name}");
                            
                            if (CameraController.Instance != null)
                            {
                                CameraController.Instance.FocusTo(_pressedBuilding.transform.position, 6.0f, 0.5f);
                            }

                            if (_pressedBuilding.CurrentState == BuildingState.ReadyToHarvest || _pressedBuilding.HasResourcesToHarvest())
                            {
                                _pressedBuilding.CollectResourcesVisually();
                            }

                            if (UIManager.Instance != null)
                            {
                                UIManager.Instance.OpenPanel("UI_BuildingActionPanel", _pressedBuilding, false);
                            }
                        }
                    }
                }

                // Nhấc ngón tay lên (Release) -> Nếu click nhanh thì mở Detail Panel
                if (inputManager.IsPrimaryPointerReleased())
                {
                    if (_pressedBuilding != null && !_hasTriggeredHold)
                    {
                        float dragDist = Vector2.Distance(_pointerDownPosition, pointerPos);
                        if (dragDist < 20f) // Khoảng cách nhỏ -> Tap thực sự
                        {
                            Debug.Log($"[BuildingController] Tap nhanh toà nhà -> Mở Detail Panel: {_pressedBuilding.name}");
                            
                            if (CameraController.Instance != null)
                            {
                                CameraController.Instance.FocusTo(_pressedBuilding.transform.position, 6.0f, 0.5f);
                            }

                            if (_pressedBuilding.CurrentState == BuildingState.ReadyToHarvest || _pressedBuilding.HasResourcesToHarvest())
                            {
                                _pressedBuilding.CollectResourcesVisually();
                            }

                            if (UIManager.Instance != null)
                            {
                                // Đóng các panel action cũ nếu có
                                UIManager.Instance.ClosePanel("UI_BuildingActionPanel");
                                UIManager.Instance.OpenPanel("UI_BuildingDetailPanel", _pressedBuilding, false);
                            }
                        }
                    }
                    else if (_pressedBuilding == null)
                    {
                        // Thả chuột ở khoảng trống -> Đóng các panel
                        if (UIManager.Instance != null)
                        {
                            UIManager.Instance.ClosePanel("UI_BuildingActionPanel");
                        }
                    }

                    _pressedBuilding = null;
                }
                return;
            }
                                                            
                                                            // --- BẮT ĐẦU PHẦN ĐẶT NHÀ (PLACING MODE) ---
                                                            
                                                            // Hiện Grid khi đặt nhà
                                                            GridManager.Instance.SetGridAlpha(1f);
                                                
                                                            bool isPressed = inputManager.IsPrimaryPointerPressed();
                                                            bool wasPressedThisFrame = inputManager.IsPrimaryPointerDown();
                                                            bool wasReleasedThisFrame = inputManager.IsPrimaryPointerReleased();
                                                
                                                            if (_isPreviewAttachedToMouse)
                                                            {
                                                                UpdatePreviewPosition(pointerPos);
                                                
                                                                // Click hoặc Thả ra để đặt toà nhà xuống map
                                                                if (wasPressedThisFrame || wasReleasedThisFrame)
                                                                {
                                                                    if (UnityEngine.EventSystems.EventSystem.current == null || 
                                                                        !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                                                                    {
                                                                        _isPreviewAttachedToMouse = false;
                                                                        SetPreviewPosition(_previewGridPos);
                                                                        Debug.Log($"[BuildingController] Thả toà nhà bám chuột tại: {_previewGridPos}");
                                                                        
                                                                        if (UIManager.Instance != null)
                                                                        {
                                                                            UIManager.Instance.OpenPanel("UI_BuildingActionPanel", this, false);
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                // Khi toà nhà đang ở trên map (chưa bám chuột)
                                                                if (wasPressedThisFrame)
                                                                {
                                                                    _isPointerDownInsidePreview = false;
                                                                    Ray ray = Camera.main.ScreenPointToRay(pointerPos);
                                                                    Plane groundPlane = new Plane(Vector3.forward, Vector3.zero);
                                                                    if (groundPlane.Raycast(ray, out float enter))
                                                                    {
                                                                        Vector3 clickWorldPos = ray.GetPoint(enter);
                                                                        Vector2Int clickGridPos = BaseGridManager.Instance.WorldToGridPosition(clickWorldPos);
                                                                        bool clickedInside = clickGridPos.x >= _previewGridPos.x && 
                                                                                             clickGridPos.x < _previewGridPos.x + _currentWidth && 
                                                                                             clickGridPos.y >= _previewGridPos.y && 
                                                                                             clickGridPos.y < _previewGridPos.y + _currentHeight;
                                                                        if (clickedInside)
                                                                        {
                                                                            _isDraggingPreview = false;
                                                                            _isPointerDownInsidePreview = true;
                                                                        }
                                                                    }
                                                                }
                                                
                                                                if (isPressed && _isPointerDownInsidePreview)
                {
                    float dragDist = Vector2.Distance(_pointerDownPosition, pointerPos);
                    if (!_isDraggingPreview && dragDist > 15f) // Kéo xa hơn 15px
                    {
                        _isDraggingPreview = true;
                        Debug.Log("[BuildingController] Bắt đầu Kéo rê toà nhà!");
                        if (UIManager.Instance != null)
                        {
                            UIManager.Instance.ClosePanel("UI_BuildingActionPanel");
                        }
                    }

                    if (_isDraggingPreview)
                    {
                        UpdatePreviewPosition(pointerPos);
                    }
                }

                if (wasReleasedThisFrame)
                {
                    if (_isPointerDownInsidePreview)
                    {
                        if (_isDraggingPreview)
                        {
                            _isDraggingPreview = false;
                            Debug.Log($"[BuildingController] Thả kéo rê tại: {_previewGridPos}");
                            SetPreviewPosition(_previewGridPos);
                            if (UIManager.Instance != null)
                            {
                                UIManager.Instance.OpenPanel("UI_BuildingActionPanel", this, false);
                            }
                        }
                        else
                        {
                            // Thả mà chưa từng kéo -> Tapped
                            float tapDist = Vector2.Distance(_pointerDownPosition, pointerPos);
                            if (tapDist < 15f)
                            {
                                _isPreviewAttachedToMouse = true;
                                Debug.Log("[BuildingController] Nhấc toà nhà bám theo chuột thành công!");
                                if (UIManager.Instance != null)
                                {
                                    UIManager.Instance.ClosePanel("UI_BuildingActionPanel");
                                }
                            }
                        }
                    }
                    _isPointerDownInsidePreview = false;
                }
            }

            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.rKey.wasPressedThisFrame)
                {
                    RotateBuilding();
                }
                if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    CancelPlacement();
                }
                if (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
                {
                    TryPlaceBuilding();
                }
            }

            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null && mouse.rightButton.wasPressedThisFrame)
            {
                CancelPlacement();
            }
        }

        public void StartPlacement(string buildingId, string prefabAddress, int width, int height, long instanceId = 0, int level = 1)
        {
            _buildingIdToPlace = buildingId;
            _prefabAddressToPlace = prefabAddress;
            _currentWidth = width;
            _currentHeight = height;
            _isPlacing = true;
            _isMovingExisting = false;
            _instanceIdToPlace = instanceId;
            _levelToPlace = level;
            _currentFlipX = false;
            _hasGhostLoaded = false;
            _isDraggingPreview = false;
            _isPreviewAttachedToMouse = !DeviceManager.Instance.IsMobile; // PC thì dính chuột ngay, Mobile thì không dính chuột (phải chạm kéo)

            if (previewRenderer != null)
            {
                previewRenderer.gameObject.SetActive(true);
                previewRenderer.flipX = _currentFlipX;
                
                if (BaseBuildingManager.Instance != null && 
                    BaseBuildingManager.Instance.GetBuildingDatabase().TryGetValue(buildingId, out BuildingData data))
                {
                    if (data != null && data.VisualConfig != null)
                    {
                        var vis = data.VisualConfig.GetVisualsForLevel(1);
                        if (vis != null && vis.ghostSprite != null)
                        {
                            previewRenderer.sprite = vis.ghostSprite;
                            _hasGhostLoaded = true;

                            if (vis.normalSprite != null)
                            {
                                float normalWidth = vis.normalSprite.rect.width / vis.normalSprite.pixelsPerUnit;
                                float normalHeight = vis.normalSprite.rect.height / vis.normalSprite.pixelsPerUnit;
                                float ghostWidth = vis.ghostSprite.rect.width / vis.ghostSprite.pixelsPerUnit;
                                float ghostHeight = vis.ghostSprite.rect.height / vis.ghostSprite.pixelsPerUnit;
                                
                                float scaleX = ghostWidth > 0 ? (normalWidth / ghostWidth) : 1f;
                                float scaleY = ghostHeight > 0 ? (normalHeight / ghostHeight) : 1f;
                                
                                previewRenderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);
                            }
                            else
                            {
                                previewRenderer.transform.localScale = Vector3.one;
                            }
                        }
                    }
                }
                
                if (!_hasGhostLoaded)
                {
                    float cellSize = 1f;
                    previewRenderer.transform.localScale = new Vector3(width * cellSize, height * cellSize, 1);
                }
            }

            // Đặt vị trí preview mặc định tại tâm camera
            Vector2Int startGrid = new Vector2Int(0, 0);
            if (Camera.main != null)
            {
                Vector3 centerWorld = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, 10f));
                startGrid = BaseGridManager.Instance.WorldToGridPosition(centerWorld);
            }
            SetPreviewPosition(startGrid);
            if (CameraController.Instance != null)
            {
                Vector3 worldPos = BaseGridManager.Instance.GridToWorldPosition(startGrid.x, startGrid.y);
                CameraController.Instance.FocusTo(worldPos, 6.0f, 0.5f);
            }
        }

        public void StartMove(BuildingInstance building)
        {
            if (building == null) return;

            _buildingIdToPlace = building.Data.BuildingID;
            _prefabAddressToPlace = building.Data.PrefabAddress;
            _currentWidth = building.CurrentSizeX;
            _currentHeight = building.CurrentSizeY;
            _currentFlipX = building.FlipX;
            _isPlacing = true;
            _hasGhostLoaded = false;
            _isDraggingPreview = false;
            _isPreviewAttachedToMouse = !DeviceManager.Instance.IsMobile; // Di chuyển thì PC bám theo chuột ngay, Mobile thì không

            _isMovingExisting = true;
            _movingBuildingId = building.Data.BuildingID;
            _movingOriginalX = building.GridX;
            _movingOriginalY = building.GridY;
            _movingOriginalLevel = building.CurrentLevel;
            _movingOriginalFlipX = building.FlipX;
            _movingOriginalInstanceId = building.InstanceID;

            if (previewRenderer != null)
            {
                previewRenderer.gameObject.SetActive(true);
                previewRenderer.flipX = _currentFlipX;
                
                Vector3 baseScale = building.transform.localScale;

                if (building.Data.VisualConfig != null)
                {
                    var vis = building.Data.VisualConfig.GetVisualsForLevel(building.CurrentLevel);
                    if (vis != null && vis.ghostSprite != null)
                    {
                        previewRenderer.sprite = vis.ghostSprite;
                        _hasGhostLoaded = true;

                        if (vis.normalSprite != null)
                        {
                            float normalWidth = vis.normalSprite.rect.width / vis.normalSprite.pixelsPerUnit;
                            float normalHeight = vis.normalSprite.rect.height / vis.normalSprite.pixelsPerUnit;
                            float ghostWidth = vis.ghostSprite.rect.width / vis.ghostSprite.pixelsPerUnit;
                            float ghostHeight = vis.ghostSprite.rect.height / vis.ghostSprite.pixelsPerUnit;
                            
                            float scaleX = ghostWidth > 0 ? (normalWidth / ghostWidth) : 1f;
                            float scaleY = ghostHeight > 0 ? (normalHeight / ghostHeight) : 1f;
                            
                            previewRenderer.transform.localScale = new Vector3(baseScale.x * scaleX, baseScale.y * scaleY, baseScale.z);
                        }
                        else
                        {
                            previewRenderer.transform.localScale = baseScale;
                        }
                    }
                }

                if (!_hasGhostLoaded)
                {
                    float cellSize = 1f;
                    previewRenderer.transform.localScale = new Vector3(_currentWidth * cellSize, _currentHeight * cellSize, 1);
                }
            }

            SetPreviewPosition(new Vector2Int(building.GridX, building.GridY));
            if (CameraController.Instance != null)
            {
                CameraController.Instance.FocusTo(building.transform.position, 6.0f, 0.5f);
            }

            // Xóa toà nhà cũ khỏi map
            BaseBuildingManager.Instance.RemoveBuilding(building);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ClosePanel("UI_BuildingActionPanel");
            }
        }

        public void RotateBuilding()
        {
            int temp = _currentWidth;
            _currentWidth = _currentHeight;
            _currentHeight = temp;

            _currentFlipX = !_currentFlipX;
            
            if (previewRenderer != null)
            {
                previewRenderer.flipX = _currentFlipX;
                if (!_hasGhostLoaded)
                {
                    float cellSize = 1f;
                    previewRenderer.transform.localScale = new Vector3(_currentWidth * cellSize, _currentHeight * cellSize, 1);
                }
            }
            
            SetPreviewPosition(_previewGridPos);
        }

        private void SetPreviewPosition(Vector2Int gridPos)
        {
            gridPos.x = Mathf.Clamp(gridPos.x, 0, BaseGridManager.Instance.Width - _currentWidth);
            gridPos.y = Mathf.Clamp(gridPos.y, 0, BaseGridManager.Instance.Height - _currentHeight);
            _previewGridPos = gridPos;
            
            if (previewRenderer != null)
            {
                float centerX = gridPos.x + _currentWidth / 2f;
                float centerY = gridPos.y + _currentHeight / 2f;
                Vector3 worldPos = BaseGridManager.Instance.GridToWorldPosition(centerX, centerY);
                previewRenderer.transform.position = new Vector3(worldPos.x, worldPos.y, -0.5f);
                
                if (BaseGridManager.Instance.IsSpaceAvailable(gridPos.x, gridPos.y, _currentWidth, _currentHeight))
                    previewRenderer.color = validColor;
                else
                    previewRenderer.color = invalidColor;
            }
        }

        private void UpdatePreviewPosition(Vector2 pointerPos)
        {
            Plane groundPlane = new Plane(Vector3.forward, Vector3.zero);
            Ray ray = Camera.main.ScreenPointToRay(pointerPos);
            if (groundPlane.Raycast(ray, out float enter))
            {
                Vector3 worldHit = ray.GetPoint(enter);
                Vector2Int gridPos = BaseGridManager.Instance.WorldToGridPosition(worldHit);
                SetPreviewPosition(gridPos);
            }
        }

        public async void TryPlaceBuilding()
        {
            if (BaseGridManager.Instance.IsSpaceAvailable(_previewGridPos.x, _previewGridPos.y, _currentWidth, _currentHeight))
            {
                string bId = string.IsNullOrEmpty(_buildingIdToPlace) ? "UnknownBuilding" : _buildingIdToPlace;
                
                int level = _levelToPlace;
                long instanceId = _instanceIdToPlace;
                if (_isMovingExisting)
                {
                    level = _movingOriginalLevel;
                    instanceId = _movingOriginalInstanceId;
                }
                
                _isMovingExisting = false;

                bool success = await BaseBuildingManager.Instance.PlaceBuilding(bId, _previewGridPos.x, _previewGridPos.y, level, BuildingState.Normal, _currentFlipX, instanceId);
                
                if (success)
                {
                    Debug.Log($"[BuildingController] Đã đặt nhà {bId} tại {_previewGridPos.x}, {_previewGridPos.y} với lv {level}");
                    await BaseBuildingManager.Instance.SaveLayoutToServer();
                    CancelPlacement();
                }
                else
                {
                    Debug.LogWarning("[BuildingController] BaseBuildingManager từ chối đặt nhà!");
                }
            }
            else
            {
                Debug.LogWarning("[BuildingController] Không thể đặt nhà ở đây!");
            }
        }

        public void CancelPlacement()
        {
            if (!_isPlacing) return;
            
            _isPlacing = false;
            _buildingIdToPlace = null;
            _prefabAddressToPlace = null;
            if (previewRenderer != null)
            {
                previewRenderer.gameObject.SetActive(false);
            }
            
            GridManager.Instance.SetGridAlpha(0f);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ClosePanel("UI_BuildingActionPanel");
            }

            // Khôi phục toà nhà cũ về vị trí ban đầu
            if (_isMovingExisting)
            {
                _isMovingExisting = false;
                if (BaseBuildingManager.Instance != null)
                {
                    _ = BaseBuildingManager.Instance.PlaceBuilding(
                        _movingBuildingId, 
                        _movingOriginalX, 
                        _movingOriginalY, 
                        _movingOriginalLevel, 
                        BuildingState.Normal, 
                        _movingOriginalFlipX,
                        _movingOriginalInstanceId
                    );
                }
            }
        }
    }
}
