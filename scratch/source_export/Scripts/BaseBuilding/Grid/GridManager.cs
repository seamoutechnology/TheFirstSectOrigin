using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.Tilemaps;

namespace GameClient.BaseBuilding.Grid
{
    public enum CellState
    {
        Empty,
        Occupied,
        Fog // Bị sương mù che phủ (chưa mở khóa)
    }

    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance { get; private set; }

        [Header("Grid Config")]
        public int gridWidth = 100;
        public int gridHeight = 100;
        public float cellSize = 1f;
        
        public Vector2 originPosition = Vector2.zero;

        [Header("Grid Visuals")]
        public GameObject gridLinePrefab; // Prefab của Line (1 pixel màu trắng mờ)
        public Transform gridVisualsParent;
        private List<SpriteRenderer> _gridLines = new List<SpriteRenderer>();

        private CellState[,] _gridArray;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            InitializeGrid();
        }

        private TilemapRenderer _isoTilemapRenderer;

        private void Start()
        {
            GenerateIsometricGridVisuals();
            SetGridAlpha(0f); // Ẩn lưới lúc đầu
        }

        private void GenerateIsometricGridVisuals()
        {
            // 1. Tạo Texture2D hình thoi 128x64 động trong bộ nhớ
            int width = 128;
            int height = 64;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Công thức vẽ viền thoi Isometric
                    float val = Mathf.Abs((x - 63.5f) / 64f) + Mathf.Abs((y - 31.5f) / 32f);
                    if (Mathf.Abs(val - 1.0f) < 0.025f)
                    {
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, 0.4f)); // Viền trắng mờ 40%
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.filterMode = FilterMode.Bilinear;
            tex.Apply();

            // 2. Tạo Sprite với PPU = 128
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 128f);

            // 3. Tạo Tile Asset động
            Tile tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;

            // 4. Tìm hoặc tự sinh GameObject GridOverlayTilemap dưới Grid
            UnityEngine.Grid gridComponent = GetComponentInParent<UnityEngine.Grid>();
            if (gridComponent == null)
            {
                gridComponent = FindFirstObjectByType<UnityEngine.Grid>();
            }

            if (gridComponent == null)
            {
                Debug.LogError("[GridManager] Không tìm thấy Grid component!");
                return;
            }

            Transform overlayTransform = gridComponent.transform.Find("GridOverlayTilemap");
            GameObject overlayObj;
            if (overlayTransform == null)
            {
                overlayObj = new GameObject("GridOverlayTilemap");
                overlayObj.transform.SetParent(gridComponent.transform, false);
            }
            else
            {
                overlayObj = overlayTransform.gameObject;
            }

            Tilemap tilemap = overlayObj.GetComponent<Tilemap>();
            if (tilemap == null) tilemap = overlayObj.AddComponent<Tilemap>();

            _isoTilemapRenderer = overlayObj.GetComponent<TilemapRenderer>();
            if (_isoTilemapRenderer == null) _isoTilemapRenderer = overlayObj.AddComponent<TilemapRenderer>();
            
            _isoTilemapRenderer.sortOrder = TilemapRenderer.SortOrder.TopRight;
            _isoTilemapRenderer.sortingOrder = -50; // Vẽ đè lên trên đất, dưới chân nhà

            var defaultMaterial = Shader.Find("Sprites/Default") != null ? new Material(Shader.Find("Sprites/Default")) : null;
            if (defaultMaterial != null)
            {
                _isoTilemapRenderer.material = defaultMaterial;
            }

            // 5. Quét phủ toàn bộ lưới bản đồ tự động
            tilemap.ClearAllTiles();
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }

        public void SetGridAlpha(float targetAlpha)
        {
            if (_isoTilemapRenderer != null)
            {
                _isoTilemapRenderer.gameObject.SetActive(targetAlpha > 0f);
                Color c = _isoTilemapRenderer.material.color;
                c.a = targetAlpha;
                _isoTilemapRenderer.material.color = c;
            }
        }

        private void InitializeGrid()
        {
            _gridArray = new CellState[gridWidth, gridHeight];
            
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    _gridArray[x, y] = CellState.Empty; 
                }
            }
        }

        [System.Serializable]
        public class PlacedBuildingData
        {
            public string buildingId; // ID loại nhà (ví dụ "NhaChinh", "LuyenDanPhong")
            public int x, y;
            public int width, height;
        }

        public List<PlacedBuildingData> placedBuildings = new List<PlacedBuildingData>();

        public void UnlockArea(int startX, int startY, int width, int height)
        {
            for (int x = startX; x < startX + width; x++)
            {
                for (int y = startY; y < startY + height; y++)
                {
                    if (IsValidGridPosition(x, y))
                    {
                        _gridArray[x, y] = CellState.Empty;
                    }
                }
            }
        }

        public Vector2 GetWorldPosition(int x, int y)
        {
            return new Vector2(x, y) * cellSize + originPosition;
        }

        public Vector2Int GetGridPosition(Vector2 worldPosition)
        {
            int x = Mathf.FloorToInt((worldPosition.x - originPosition.x) / cellSize);
            int y = Mathf.FloorToInt((worldPosition.y - originPosition.y) / cellSize);
            return new Vector2Int(x, y);
        }

        public bool CanPlaceBuilding(int startX, int startY, int width, int height)
        {
            for (int x = startX; x < startX + width; x++)
            {
                for (int y = startY; y < startY + height; y++)
                {
                    if (!IsValidGridPosition(x, y)) return false; // Ra ngoài bản đồ
                    if (_gridArray[x, y] != CellState.Empty) return false; // Vướng nhà hoặc sương mù
                }
            }
            return true;
        }

        public void PlaceBuilding(string buildingId, int startX, int startY, int width, int height)
        {
            if (!CanPlaceBuilding(startX, startY, width, height)) return;

            for (int x = startX; x < startX + width; x++)
            {
                for (int y = startY; y < startY + height; y++)
                {
                    _gridArray[x, y] = CellState.Occupied;
                }
            }
            
            placedBuildings.Add(new PlacedBuildingData
            {
                buildingId = buildingId,
                x = startX,
                y = startY,
                width = width,
                height = height
            });
        }

        public void RemoveBuilding(int startX, int startY, int width, int height)
        {
            for (int x = startX; x < startX + width; x++)
            {
                for (int y = startY; y < startY + height; y++)
                {
                    if (IsValidGridPosition(x, y) && _gridArray[x, y] == CellState.Occupied)
                    {
                        _gridArray[x, y] = CellState.Empty;
                    }
                }
            }
            
            placedBuildings.RemoveAll(b => b.x == startX && b.y == startY);
        }

        private bool IsValidGridPosition(int x, int y)
        {
            return x >= 0 && y >= 0 && x < gridWidth && y < gridHeight;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || _gridArray == null) return;

            Gizmos.color = new Color(1, 1, 1, 0.2f);
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector2 pos = GetWorldPosition(x, y);
                    Gizmos.DrawWireCube(pos + new Vector2(cellSize / 2, cellSize / 2), new Vector3(cellSize, cellSize, 0));
                    
                    if (_gridArray[x,y] == CellState.Fog)
                    {
                        Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                        Gizmos.DrawCube(pos + new Vector2(cellSize / 2, cellSize / 2), new Vector3(cellSize, cellSize, 0));
                        Gizmos.color = new Color(1, 1, 1, 0.2f);
                    }
                    else if (_gridArray[x,y] == CellState.Occupied)
                    {
                        Gizmos.color = new Color(1, 0, 0, 0.5f);
                        Gizmos.DrawCube(pos + new Vector2(cellSize / 2, cellSize / 2), new Vector3(cellSize, cellSize, 0));
                        Gizmos.color = new Color(1, 1, 1, 0.2f);
                    }
                }
            }
        }
    }
}
