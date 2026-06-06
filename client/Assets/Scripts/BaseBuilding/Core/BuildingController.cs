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

        private bool _isPlacing;
        private int _currentWidth = 2;
        private int _currentHeight = 2;
        private string _prefabAddressToPlace;
        private string _buildingIdToPlace;

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
            }

            if (!_isPlacing)
            {
                if (inputManager.IsPrimaryPointerDown())
                {
                    Debug.Log($"[BuildingController] Nhấn chuột tại: {pointerPos}");

                    if (UnityEngine.EventSystems.EventSystem.current != null && 
                        UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    {
                        var pointerData = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
                        pointerData.position = pointerPos;
                        var results = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                        UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointerData, results);
                        
                        bool blockedByRealUI = false;
                        foreach (var result in results)
                        {
                            var parentCanvas = result.gameObject.GetComponentInParent<Canvas>();
                            if (result.gameObject.layer == 5 || parentCanvas != null)
                            {
                                // Bỏ qua nếu Canvas ở chế độ WorldSpace (như nhãn tên toà nhà hiển thị trong map)
                                if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
                                {
                                    continue;
                                }

                                var isBuildingObj = result.gameObject.GetComponent<BuildingInstance>() != null || 
                                                    result.gameObject.GetComponentInParent<BuildingInstance>() != null;
                                if (!isBuildingObj)
                                {
                                    blockedByRealUI = true;
                                    Debug.Log($"[BuildingController] Bị chặn bởi UI thực sự: {result.gameObject.name} (Layer: {result.gameObject.layer})");
                                    break;
                                }
                            }
                        }

                        if (blockedByRealUI)
                        {
                            return;
                        }
                    }

                    if (Camera.main != null)
                    {
                        Ray ray = Camera.main.ScreenPointToRay(pointerPos);
                        RaycastHit2D hit = Physics2D.GetRayIntersection(ray);

                        if (hit.collider != null)
                        {
                            Debug.Log($"[BuildingController] Raycast trúng: {hit.collider.gameObject.name} (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
                            var building = hit.collider.GetComponent<BuildingInstance>();
                            if (building == null)
                            {
                                building = hit.collider.GetComponentInParent<BuildingInstance>();
                            }

                            if (building != null)
                            {
                                Debug.Log($"[BuildingController] Tìm thấy BuildingInstance: {building.name}");
                                if (building.CurrentState == BuildingState.ReadyToHarvest || building.HasResourcesToHarvest())
                                {
                                    building.CollectResourcesVisually();
                                }

                                if (UIManager.Instance != null)
                                {
                                    Debug.Log("[BuildingController] Gọi UIManager.OpenPanel(UI_BuildingActionPanel)");
                                    UIManager.Instance.OpenPanel("UI_BuildingActionPanel", building, false);
                                }
                            }
                            else
                            {
                                Debug.Log("[BuildingController] Đối tượng trúng không phải là BuildingInstance.");
                                if (UIManager.Instance != null)
                                {
                                    UIManager.Instance.ClosePanel("UI_BuildingActionPanel");
                                }
                            }
                        }
                        else
                        {
                            Debug.Log("[BuildingController] Raycast không trúng collider nào.");
                            if (UIManager.Instance != null)
                            {
                                UIManager.Instance.ClosePanel("UI_BuildingActionPanel");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[BuildingController] Camera.main bị NULL!");
                    }
                }
                return;
            }

            // --- BẮT ĐẦU PHẦN ĐẶT NHÀ (PLACING MODE) ---
            
            // Hiện Grid khi đặt nhà
            GridManager.Instance.SetGridAlpha(1f);

            var pointer = UnityEngine.InputSystem.Pointer.current;
            bool isPressed = pointer != null && pointer.press.isPressed;
            bool wasPressedThisFrame = pointer != null && pointer.press.wasPressedThisFrame;
            bool wasReleasedThisFrame = pointer != null && pointer.press.wasReleasedThisFrame;

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

        public void StartPlacement(string buildingId, string prefabAddress, int width, int height)
        {
            _buildingIdToPlace = buildingId;
            _prefabAddressToPlace = prefabAddress;
            _currentWidth = width;
            _currentHeight = height;
            _isPlacing = true;
            _isMovingExisting = false;
            _currentFlipX = false;
            _hasGhostLoaded = false;
            _isDraggingPreview = false;
            _isPreviewAttachedToMouse = true; // Xây mới thì dính chuột ngay

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
            _isPreviewAttachedToMouse = true; // Di chuyển thì lập tức bám theo chuột

            _isMovingExisting = true;
            _movingBuildingId = building.Data.BuildingID;
            _movingOriginalX = building.GridX;
            _movingOriginalY = building.GridY;
            _movingOriginalLevel = building.CurrentLevel;
            _movingOriginalFlipX = building.FlipX;

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
                float worldX = (gridPos.x + _currentWidth / 2f) * BaseGridManager.TILE_WIDTH;
                float worldY = (gridPos.y + _currentHeight / 2f) * BaseGridManager.TILE_HEIGHT;
                previewRenderer.transform.position = new Vector3(worldX, worldY, -0.5f);
                
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
                int level = _isMovingExisting ? _movingOriginalLevel : 1;
                
                _isMovingExisting = false;

                bool success = await BaseBuildingManager.Instance.PlaceBuilding(bId, _previewGridPos.x, _previewGridPos.y, level, BuildingState.Normal, _currentFlipX);
                
                if (success)
                {
                    Debug.Log($"[BuildingController] Đã đặt nhà {bId} tại {_previewGridPos.x}, {_previewGridPos.y} với lv {level}");
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
                        _movingOriginalFlipX
                    );
                }
            }
        }
    }
}
