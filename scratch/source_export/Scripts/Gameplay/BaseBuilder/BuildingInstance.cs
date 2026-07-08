using GameClient.Core;
using GameClient.Managers;
using UnityEngine;

namespace GameClient.Gameplay.BaseBuilder
{
    public class BuildingInstance : MonoBehaviour
    {
        public BuildingData Data { get; private set; }
        public long InstanceID { get; private set; }
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public int CurrentLevel { get; private set; }
        public BuildingState CurrentState { get; private set; }
        public bool FlipX { get; private set; }

        public int CurrentSizeX => Data != null ? (FlipX ? Data.SizeY : Data.SizeX) : 1;
        public int CurrentSizeY => Data != null ? (FlipX ? Data.SizeX : Data.SizeY) : 1;

        private SpriteRenderer _spriteRenderer;
        private GameObject _currentVFX;
        private Coroutine _constructionSoundCoroutine;

        public void UpdateInstanceID(long instanceId)
        {
            InstanceID = instanceId;
        }

        public void Setup(BuildingData data, int x, int y, int level = 1, BuildingState state = BuildingState.Normal, bool flipX = false, long instanceId = 0)
        {
            Data = data;
            InstanceID = instanceId;
            GridX = x;
            GridY = y;
            CurrentLevel = level;
            CurrentState = state;
            if (Data is ProductionBuildingData && CurrentState == BuildingState.Normal)
            {
                CurrentState = BuildingState.Producing;
            }
            FlipX = flipX;
            
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            var oldCol = GetComponent<BoxCollider2D>();
            if (oldCol != null) Destroy(oldCol);

            var col = GetComponent<PolygonCollider2D>();
            if (col == null)
            {
                col = gameObject.AddComponent<PolygonCollider2D>();
            }

            _spriteRenderer.flipX = FlipX;
            
            UpdateVisualSprite();
            UpdateVisualPosition();

            if (CurrentState == BuildingState.Building || CurrentState == BuildingState.Upgrading)
            {
                if (_constructionSoundCoroutine != null) StopCoroutine(_constructionSoundCoroutine);
                _constructionSoundCoroutine = StartCoroutine(Co_PeriodicConstructionSound());
            }
        }

        public void SetState(BuildingState newState)
        {
            BuildingState oldState = CurrentState;
            CurrentState = newState;
            UpdateVisualSprite();
            HandleStateAudioTransitions(oldState, CurrentState);
        }

        public void SetLevel(int newLevel)
        {
            CurrentLevel = newLevel;
            UpdateVisualSprite();
        }

        private void UpdateVisualSprite()
        {
            if (Data.VisualConfig != null)
            {
                var vis = Data.VisualConfig.GetVisualsForLevel(CurrentLevel);
                if (vis != null)
                {
                    switch (CurrentState)
                    {
                        case BuildingState.Normal: _spriteRenderer.sprite = vis.normalSprite; break;
                        case BuildingState.Ghost: _spriteRenderer.sprite = vis.ghostSprite; break;
                        case BuildingState.Building: _spriteRenderer.sprite = vis.buildingSprite; break;
                        case BuildingState.Upgrading: _spriteRenderer.sprite = vis.upgradingSprite; break;
                        case BuildingState.Broken: _spriteRenderer.sprite = vis.brokenSprite; break;
                        case BuildingState.Locked: _spriteRenderer.sprite = vis.lockedSprite; break;
                        case BuildingState.Producing: _spriteRenderer.sprite = vis.producingSprite; break;
                        case BuildingState.ReadyToHarvest: _spriteRenderer.sprite = vis.readyToHarvestSprite; break;
                    }

                    _spriteRenderer.flipX = FlipX;
                }
                
                UpdateVFX();
            }

            if (_spriteRenderer != null && _spriteRenderer.sprite != null && Data != null)
            {
                Vector3 spriteSize = _spriteRenderer.sprite.bounds.size;
                if (spriteSize.x > 0 && spriteSize.y > 0)
                {
                    float targetWidth = Data.SizeX * BaseGridManager.TILE_WIDTH;
                    float targetHeight = Data.SizeY * BaseGridManager.TILE_HEIGHT;
                    transform.localScale = new Vector3(targetWidth / spriteSize.x, targetHeight / spriteSize.y, 1f);
                }
            }
            UpdateColliderShape();
        }

        private void UpdateColliderShape()
        {
            var col = GetComponent<PolygonCollider2D>();
            if (col == null || BaseGridManager.Instance == null || Data == null) return;

            Vector3 worldBottom = BaseGridManager.Instance.GridToWorldPosition(GridX, GridY);
            Vector3 worldTop = BaseGridManager.Instance.GridToWorldPosition(GridX + CurrentSizeX, GridY + CurrentSizeY);
            Vector3 worldLeft = BaseGridManager.Instance.GridToWorldPosition(GridX, GridY + CurrentSizeY);
            Vector3 worldRight = BaseGridManager.Instance.GridToWorldPosition(GridX + CurrentSizeX, GridY);
            Vector3 worldCenter = (worldBottom + worldTop) / 2f;

            float scaleX = transform.localScale.x != 0 ? transform.localScale.x : 1f;
            float scaleY = transform.localScale.y != 0 ? transform.localScale.y : 1f;

            Vector2[] points = new Vector2[4];
            points[0] = new Vector2((worldTop.x - worldCenter.x) / scaleX, (worldTop.y - worldCenter.y) / scaleY);     // Top
            points[1] = new Vector2((worldRight.x - worldCenter.x) / scaleX, (worldRight.y - worldCenter.y) / scaleY); // Right
            points[2] = new Vector2((worldBottom.x - worldCenter.x) / scaleX, (worldBottom.y - worldCenter.y) / scaleY); // Bottom
            points[3] = new Vector2((worldLeft.x - worldCenter.x) / scaleX, (worldLeft.y - worldCenter.y) / scaleY);   // Left
            
            col.points = points;
        }

        private void UpdateVFX()
        {
            if (Data.VisualConfig == null) return;

            if (_currentVFX != null)
            {
                Destroy(_currentVFX);
                _currentVFX = null;
            }

            GameObject vfxPrefab = null;

            if (CurrentState == BuildingState.Building || CurrentState == BuildingState.Upgrading)
            {
                vfxPrefab = Data.VisualConfig.constructionVFXPrefab;
            }
            else if (CurrentState == BuildingState.Producing)
            {
                vfxPrefab = Data.VisualConfig.workingVFXPrefab;
            }

            if (vfxPrefab != null)
            {
                _currentVFX = Instantiate(vfxPrefab, transform);
                
            }
        }

        public void MoveTo(int newX, int newY)
        {
            GridX = newX;
            GridY = newY;
            UpdateVisualPosition();
        }

        private void UpdateVisualPosition()
        {
            if (BaseGridManager.Instance != null && Data != null)
            {
                float centerX = GridX + CurrentSizeX / 2f;
                float centerY = GridY + CurrentSizeY / 2f;
                Vector3 worldPos = BaseGridManager.Instance.GridToWorldPosition(centerX, centerY);
                transform.position = new Vector3(worldPos.x, worldPos.y, 0);
                
                if (_spriteRenderer != null)
                {
                    _spriteRenderer.sortingOrder = 10000 - (GridX + GridY) * 10;
                }
            }
        }

        private float _currentStorage = 0f;
        
        private GameObject _progressBarGO;
        private UnityEngine.UI.Slider _progressBarSlider;
        private TMPro.TMP_Text _progressBarText;
        private long _upgradeStartAt;

        public long UpgradeEndAt { get; private set; }

        public void SyncUpgradeState(int level, long upgradeEndAt, BuildingState state)
        {
            BuildingState oldState = CurrentState;
            CurrentLevel = level;
            UpgradeEndAt = upgradeEndAt;
            CurrentState = state;
            UpdateVisualSprite();
            
            if (CurrentState == BuildingState.Upgrading)
            {
                CreateUpgradeProgressBar();
            }
            else
            {
                DestroyUpgradeProgressBar();
            }

            HandleStateAudioTransitions(oldState, CurrentState);
        }

        private void CreateUpgradeProgressBar()
        {
            if (_progressBarGO != null) return;

            // 1. Create World Space Canvas container first so the UI components have a Canvas to render in
            var canvasGO = new GameObject("UpgradeProgressBarCanvas");
            canvasGO.transform.SetParent(transform, false);
            
            // Calculate exact visual height from the pivot to the top of the sprite
            float localYOffset = CurrentSizeY / 2f + 0.8f; // fallback
            if (_spriteRenderer != null && _spriteRenderer.sprite != null)
            {
                localYOffset = _spriteRenderer.bounds.max.y - transform.position.y;
            }
            
            canvasGO.transform.localPosition = new Vector3(0, localYOffset + 0.5f, -1f);
            canvasGO.transform.localScale = new Vector3(0.01f, 0.01f, 1f); // scale down World Space canvas

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 500; // render on top of buildings

            var rectTransform = canvasGO.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(150, 40); // Standard UI dimensions for progress bar
            }
            
            var scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;

            // Try to load a prefab first (check local Resources)
            GameObject prefab = Resources.Load<GameObject>("Prefabs/UI/BuildingProgressBar");
            if (prefab == null)
            {
                prefab = Resources.Load<GameObject>("Prefabs/UI/Component/BuildingProgressBar");
            }

            if (prefab != null)
            {
                var instance = Instantiate(prefab, canvasGO.transform, false);
                var rect = instance.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = Vector2.zero;
                    rect.localScale = Vector3.one;
                }
                _progressBarGO = canvasGO;
                InitProgressBarElements();
                return;
            }

            // Background image
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(canvasGO.transform, false);
            var bgImg = bgGO.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0, 0, 0, 0.6f);
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(150, 20);

            // Fill Area
            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(canvasGO.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.sizeDelta = new Vector2(146, 16);

            // Fill
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillArea.transform, false);
            var fillImg = fillGO.AddComponent<UnityEngine.UI.Image>();
            fillImg.color = new Color(0f, 0.8f, 0.2f, 1f); // Green fill
            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            // Slider
            var slider = canvasGO.AddComponent<UnityEngine.UI.Slider>();
            slider.transition = UnityEngine.UI.Selectable.Transition.None;
            slider.fillRect = fillRect;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0f;
            _progressBarSlider = slider;

            // Text
            var textGO = new GameObject("TimeText");
            textGO.transform.SetParent(canvasGO.transform, false);
            var text = textGO.AddComponent<TMPro.TextMeshProUGUI>();
            text.fontSize = 12f;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.color = Color.white;
            var textRect = textGO.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(150, 20);
            textRect.anchoredPosition = new Vector2(0, 15); // float above progress bar
            _progressBarText = text;

            _upgradeStartAt = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int buildTime = 60; // fallback default
            if (Data != null && Data.LevelStats != null)
            {
                int nextLevel = CurrentLevel + 1;
                var stats = Data.LevelStats.Find(s => s.Level == nextLevel);
                if (stats != null && stats.BuildTimeSeconds > 0)
                {
                    buildTime = stats.BuildTimeSeconds;
                }
            }

            if (UpgradeEndAt > 0)
            {
                _upgradeStartAt = UpgradeEndAt - buildTime; 
            }
            _progressBarGO = canvasGO;
        }

        private void InitProgressBarElements()
        {
            float localYOffset = CurrentSizeY / 2f + 0.8f;
            if (_spriteRenderer != null && _spriteRenderer.sprite != null)
            {
                localYOffset = _spriteRenderer.bounds.max.y - transform.position.y;
            }
            _progressBarGO.transform.localPosition = new Vector3(0, localYOffset + 0.5f, -1f);
            _progressBarSlider = _progressBarGO.GetComponentInChildren<UnityEngine.UI.Slider>();
            _progressBarText = _progressBarGO.GetComponentInChildren<TMPro.TMP_Text>();
            if (_progressBarText == null)
            {
                _progressBarText = _progressBarGO.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            }

            _upgradeStartAt = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            int buildTime = 60; // fallback default
            if (Data != null && Data.LevelStats != null)
            {
                int nextLevel = CurrentLevel + 1;
                var stats = Data.LevelStats.Find(s => s.Level == nextLevel);
                if (stats != null && stats.BuildTimeSeconds > 0)
                {
                    buildTime = stats.BuildTimeSeconds;
                }
            }

            if (UpgradeEndAt > 0)
            {
                _upgradeStartAt = UpgradeEndAt - buildTime;
            }
        }

        private void DestroyUpgradeProgressBar()
        {
            if (_progressBarGO != null)
            {
                Destroy(_progressBarGO);
                _progressBarGO = null;
                _progressBarSlider = null;
                _progressBarText = null;
            }
        }

        private void Update()
        {
            if (Data is ProductionBuildingData prodData)
            {
                if (CurrentState == BuildingState.Normal)
                {
                    SetState(BuildingState.Producing);
                }

                if (CurrentState == BuildingState.Producing || CurrentState == BuildingState.ReadyToHarvest)
                {
                    if (_currentStorage < prodData.MaxCapacity)
                    {
                        if (CurrentState != BuildingState.Producing) SetState(BuildingState.Producing);
                        
                        _currentStorage += prodData.ProductionRatePerSecond * Time.deltaTime;
                        
                        if (_currentStorage >= prodData.MaxCapacity)
                        {
                            _currentStorage = prodData.MaxCapacity;
                            SetState(BuildingState.ReadyToHarvest);
                        }
                    }
                }
            }

            if (CurrentState == BuildingState.Upgrading && UpgradeEndAt > 0)
            {
                long now = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                long total = UpgradeEndAt - _upgradeStartAt;
                long remaining = UpgradeEndAt - now;

                if (EventManager.Instance != null)
                {
                    EventManager.Instance.Emit("ON_BUILDING_UPGRADE_TICK", this);
                }

                if (remaining <= 0)
                {
                    // ✅ FIX: tăng level +1 khi timer hết (server cũng tăng theo lazy completion)
                    SyncUpgradeState(CurrentLevel + 1, 0, BuildingState.Normal);
                    TriggerLocalBaseSync();
                    
                    // ✅ FIX: Save layout lên server ngay để tránh tòa nhà bị reset vị trí / biến mất khi out game
                    if (BaseBuildingManager.Instance != null)
                    {
                        _ = BaseBuildingManager.Instance.SaveLayoutToServer();
                    }
                }
                else
                {
                    if (_progressBarSlider != null)
                    {
                        float progress = total > 0 ? (float)(total - remaining) / total : 0f;
                        _progressBarSlider.value = Mathf.Clamp01(progress);
                    }
                    if (_progressBarText != null)
                    {
                        _progressBarText.text = $"Lv.{CurrentLevel} Nâng Cấp: {remaining}s";
                    }
                }
            }
        }

        private async void TriggerLocalBaseSync()
        {
            try
            {
                var baseResp = await GameClient.Network.Api.SectBuildingApi.GetBaseAsync();
                if (baseResp != null && baseResp.Base != null && baseResp.Base.Code == 0)
                {
                    GameManager.Instance.SetBuildings(baseResp.Buildings);
                    BaseBuildingManager.Instance.SyncBuildingsWithServerData(baseResp.Buildings);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BuildingInstance] Lỗi tự động đồng bộ sau nâng cấp: {ex.Message}");
            }
        }

        public bool IsMaxLevel()
        {
            if (Data == null || Data.LevelStats == null || Data.LevelStats.Count == 0) return false;
            int maxLevel = 0;
            foreach (var stats in Data.LevelStats)
            {
                if (stats.Level > maxLevel) maxLevel = stats.Level;
            }
            return CurrentLevel >= maxLevel;
        }

        public bool HasResourcesToHarvest()
        {
            if (Data is ProductionBuildingData prodData)
            {
                return _currentStorage > (prodData.MaxCapacity * 0.1f);
            }
            return false; 
        }

        public void CollectResourcesVisually()
        {
            if (!HasResourcesToHarvest()) return;

            int amount = Mathf.FloorToInt(_currentStorage);
            _currentStorage = 0f;
            
            Debug.Log($"[BaseBuilder] Đã thu hoạch {amount} tài nguyên từ {Data.BuildingNameKey}");
            
            SetState(BuildingState.Producing);
        }

        private void OnMouseUpAsButton()
        {
            if (CurrentState == BuildingState.ReadyToHarvest || HasResourcesToHarvest())
            {
                CollectResourcesVisually();
            }
            
            if (GameClient.UIManager.Instance != null)
            {
                GameClient.UIManager.Instance.OpenPanel("UI_BuildingActionPanel", this, false);
            }
        }

        private void OnDestroy()
        {
            if (_constructionSoundCoroutine != null)
            {
                StopCoroutine(_constructionSoundCoroutine);
                _constructionSoundCoroutine = null;
            }
            DestroyUpgradeProgressBar();
        }

        private void HandleStateAudioTransitions(BuildingState oldState, BuildingState newState)
        {
            bool wasBuilding = (oldState == BuildingState.Building || oldState == BuildingState.Upgrading);
            bool isBuilding = (newState == BuildingState.Building || newState == BuildingState.Upgrading);

            if (!wasBuilding && isBuilding)
            {
                // Play starting construction sound
                AudioManager.Instance.PlaySFX(GameClient.Core.GameConstants.Audio.SFX_BUILD_START);
                
                // Start periodic ambient sound coroutine
                if (_constructionSoundCoroutine != null)
                {
                    StopCoroutine(_constructionSoundCoroutine);
                }
                _constructionSoundCoroutine = StartCoroutine(Co_PeriodicConstructionSound());
            }
            else if (wasBuilding && !isBuilding)
            {
                // Stop periodic ambient sound
                if (_constructionSoundCoroutine != null)
                {
                    StopCoroutine(_constructionSoundCoroutine);
                    _constructionSoundCoroutine = null;
                }

                // If construction completed successfully
                if (newState == BuildingState.Normal || newState == BuildingState.Producing || newState == BuildingState.ReadyToHarvest)
                {
                    AudioManager.Instance.PlaySFX(GameClient.Core.GameConstants.Audio.SFX_BUILD_COMPLETE);
                    FocusCameraOnPlayer();
                }
            }
        }

        private void FocusCameraOnPlayer()
        {
            if (GameClient.BaseBuilding.Core.CameraController.Instance == null) return;
            
            Transform playerTrans = null;
            GameObject playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                playerTrans = playerGO.transform;
            }
            else
            {
                playerGO = GameObject.Find("Player");
                if (playerGO != null)
                {
                    playerTrans = playerGO.transform;
                }
                else
                {
                    var dummy = FindFirstObjectByType<DummySkinSwapper>();
                    if (dummy != null)
                    {
                        playerTrans = dummy.transform;
                    }
                }
            }

            if (playerTrans != null)
            {
                GameClient.BaseBuilding.Core.CameraController.Instance.FocusTo(playerTrans.position);
            }
            else
            {
                Debug.LogWarning("[BuildingInstance] Không tìm thấy nhân vật/Player để di chuyển Camera!");
            }
        }

        private System.Collections.IEnumerator Co_PeriodicConstructionSound()
        {
            while (true)
            {
                float waitTime = Random.Range(30f, 60f);
                yield return new WaitForSeconds(waitTime);
                
                if (CurrentState == BuildingState.Building || CurrentState == BuildingState.Upgrading)
                {
                    AudioManager.Instance.PlaySFX(GameClient.Core.GameConstants.Audio.SFX_BUILD_AMBIENT);
                }
                else
                {
                    break;
                }
            }
        }
    }
}
