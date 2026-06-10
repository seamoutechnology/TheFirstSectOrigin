using UnityEngine;
using GameClient.Core;

namespace GameClient.Gameplay.BaseBuilder
{
    public class BaseGridManager : Singleton<BaseGridManager>
    {
        public int Width { get; private set; } = 30;
        public int Height { get; private set; } = 30;

        private bool[,] _grid;

        private bool[,] _unbuildableGrid;

        public const float TILE_WIDTH = 1f;
        public const float TILE_HEIGHT = 1f;

        protected override void Awake()
        {
            base.Awake();
            InitializeGrid(Width, Height);
        }

        public void InitializeGrid(int width, int height)
        {
            Width = width;
            Height = height;
            _grid = new bool[Width, Height];
            _unbuildableGrid = new bool[Width, Height];
            Debug.Log($"[BaseGrid] Đã tạo Grid kích thước {Width}x{Height}");
        }

        public void LoadTerrainData(int[] terrainData, int width, int height)
        {
            if (terrainData == null || terrainData.Length == 0) return;
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int idx = y * width + x;
                    if (idx < terrainData.Length && terrainData[idx] == 1)
                    {
                        _unbuildableGrid[x, y] = true;
                    }
                }
            }
        }

        public void ExpandGrid(int newWidth, int newHeight)
        {
            if (newWidth <= Width || newHeight <= Height) return;

            bool[,] newGrid = new bool[newWidth, newHeight];
            bool[,] newUnbuildable = new bool[newWidth, newHeight];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    newGrid[x, y] = _grid[x, y];
                    newUnbuildable[x, y] = _unbuildableGrid[x, y];
                }
            }

            _grid = newGrid;
            _unbuildableGrid = newUnbuildable;
            Width = newWidth;
            Height = newHeight;
            Debug.Log($"[BaseGrid] Đã mở rộng Base thành {Width}x{Height}");
        }

        public bool IsSpaceAvailable(int startX, int startY, int sizeX, int sizeY)
        {
            if (startX < 0 || startY < 0 || startX + sizeX > Width || startY + sizeY > Height)
            {
                Debug.LogWarning($"[BaseGrid] Vượt giới hạn bản đồ: startX={startX}, startY={startY}, size={sizeX}x{sizeY}, bản đồ={Width}x{Height}");
                return false; // Ra ngoài bản đồ
            }

            for (int x = startX; x < startX + sizeX; x++)
            {
                for (int y = startY; y < startY + sizeY; y++)
                {
                    if (_unbuildableGrid[x, y])
                    {
                        Debug.LogWarning($"[BaseGrid] Ô ({x}, {y}) có địa hình cấm xây!");
                        return false; // Địa hình cấm xây
                    }
                    if (_grid[x, y])
                    {
                        Debug.LogWarning($"[BaseGrid] Ô ({x}, {y}) đã bị chiếm dụng!");
                        return false; // Đã có vật cản/công trình
                    }
                }
            }

            return true;
        }

        public void SetOccupied(int startX, int startY, int sizeX, int sizeY, bool isOccupied)
        {
            if (startX < 0 || startY < 0 || startX + sizeX > Width || startY + sizeY > Height)
                return;

            for (int x = startX; x < startX + sizeX; x++)
            {
                for (int y = startY; y < startY + sizeY; y++)
                {
                    _grid[x, y] = isOccupied;
                }
            }
        }

        public Vector3 GridToWorldPosition(float x, float y)
        {
            // Công thức dịch chuyển Isometric chéo (X dịch ngang, Y dịch chéo lên)
            float worldX = (x - y) * (TILE_WIDTH / 2f);
            float worldY = (x + y) * (TILE_HEIGHT / 4f);
            return new Vector3(worldX, worldY, 0);
        }

        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            float xOverW = worldPos.x / (TILE_WIDTH / 2f);
            float yOverH = worldPos.y / (TILE_HEIGHT / 4f);
            int gridX = Mathf.FloorToInt((xOverW + yOverH) / 2f);
            int gridY = Mathf.FloorToInt((yOverH - xOverW) / 2f);
            return new Vector2Int(gridX, gridY);
        }
    }
}
