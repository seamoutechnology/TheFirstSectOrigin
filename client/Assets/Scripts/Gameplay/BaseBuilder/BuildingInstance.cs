using UnityEngine;

namespace GameClient.Gameplay.BaseBuilder
{
    public class BuildingInstance : MonoBehaviour
    {
        public BuildingData Data { get; private set; }
        public int GridX { get; private set; }
        public int GridY { get; private set; }
        public int CurrentLevel { get; private set; }
        public BuildingState CurrentState { get; private set; }
        public bool FlipX { get; private set; }

        public int CurrentSizeX => Data != null ? (FlipX ? Data.SizeY : Data.SizeX) : 1;
        public int CurrentSizeY => Data != null ? (FlipX ? Data.SizeX : Data.SizeY) : 1;

        private SpriteRenderer _spriteRenderer;
        private GameObject _currentVFX;

        public void Setup(BuildingData data, int x, int y, int level = 1, BuildingState state = BuildingState.Normal, bool flipX = false)
        {
            Data = data;
            GridX = x;
            GridY = y;
            CurrentLevel = level;
            CurrentState = state;
            FlipX = flipX;
            
            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            var col = GetComponent<BoxCollider2D>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider2D>();
            }
            col.size = new Vector2(CurrentSizeX, CurrentSizeY); // Sáº½ tinh chá»‰nh cá»¡ sau khi cÃ³ Sprite

            _spriteRenderer.flipX = FlipX;
            
            UpdateVisualSprite();
            UpdateVisualPosition();
        }

        public void SetState(BuildingState newState)
        {
            CurrentState = newState;
            UpdateVisualSprite();
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
                float worldX = (GridX + Data.SizeX / 2f) * BaseGridManager.TILE_WIDTH;
                float worldY = (GridY + Data.SizeY / 2f) * BaseGridManager.TILE_HEIGHT;
                
                transform.position = new Vector3(worldX, worldY, 0);
                
                if (_spriteRenderer != null)
                {
                    _spriteRenderer.sortingOrder = 10000 - (GridX + GridY) * 10;
                }
            }
        }

        private float _currentStorage = 0f;

        private void Update()
        {
            if (Data is ProductionBuildingData prodData)
            {
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
    }
}
