using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

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

        private void Start()
        {
            GenerateGridVisuals();
            SetGridAlpha(0f); // Ẩn lưới lúc đầu
        }

        private void GenerateGridVisuals()
        {
            if (gridLinePrefab == null || gridVisualsParent == null) return;

            for (int x = 0; x <= gridWidth; x++)
            {
                Vector2 pos = originPosition + new Vector2(x * cellSize, (gridHeight * cellSize) / 2f);
                GameObject lineObj = Instantiate(gridLinePrefab, pos, Quaternion.identity, gridVisualsParent);
                lineObj.transform.localScale = new Vector3(0.05f, gridHeight * cellSize, 1f); // 0.05 là bề ngang của nét vẽ
                
                var sr = lineObj.GetComponent<SpriteRenderer>();
                if (sr != null) _gridLines.Add(sr);
            }

            for (int y = 0; y <= gridHeight; y++)
            {
                Vector2 pos = originPosition + new Vector2((gridWidth * cellSize) / 2f, y * cellSize);
                GameObject lineObj = Instantiate(gridLinePrefab, pos, Quaternion.identity, gridVisualsParent);
                lineObj.transform.localScale = new Vector3(gridWidth * cellSize, 0.05f, 1f);
                
                var sr = lineObj.GetComponent<SpriteRenderer>();
                if (sr != null) _gridLines.Add(sr);
            }
        }

        public void SetGridAlpha(float targetAlpha)
        {
            foreach (var line in _gridLines)
            {
                if (line != null)
                {
                    line.DOKill();
                    line.DOFade(targetAlpha, 0.3f);
                }
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
