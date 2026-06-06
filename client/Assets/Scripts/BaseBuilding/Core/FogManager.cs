using UnityEngine;
using GameClient.BaseBuilding.Grid;
using GameClient.Managers; // Dùng UIManager để hiển thị hộp thoại mở khóa

namespace GameClient.BaseBuilding.Core
{
    public class FogManager : MonoBehaviour
    {
        public static FogManager Instance { get; private set; }

        [Header("Fog Settings")]
        public GameObject fogTilePrefab; // Prefab của cục sương mù mờ ảo
        public Transform fogParent; // Gom các cụm sương mù vào 1 chỗ cho gọn

        private GameObject[,] _fogObjects;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            GenerateFogVisuals();
        }

        private void GenerateFogVisuals()
        {
            int width = GridManager.Instance.gridWidth;
            int height = GridManager.Instance.gridHeight;
            float cellSize = GridManager.Instance.cellSize;

            _fogObjects = new GameObject[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2 pos = GridManager.Instance.GetWorldPosition(x, y) + new Vector2(cellSize / 2, cellSize / 2);
                    
                    GameObject fog = Instantiate(fogTilePrefab, pos, Quaternion.identity, fogParent);
                    var col = fog.AddComponent<BoxCollider2D>();
                    col.size = new Vector2(cellSize, cellSize);
                    
                    _fogObjects[x, y] = fog;
                }
            }
        }

        public void TryUnlockArea(int startX, int startY, int width, int height)
        {
            UIManager.Instance.ShowMessage("Mở khóa sương mù", "Mở khóa khu vực này cần 1000 Linh Thạch. Mở ngay?", 
                onConfirm: () => {
                    ConfirmUnlockArea(startX, startY, width, height);
                }
            );
        }

        private void ConfirmUnlockArea(int startX, int startY, int width, int height)
        {
            GridManager.Instance.UnlockArea(startX, startY, width, height);

            for (int x = startX; x < startX + width; x++)
            {
                for (int y = startY; y < startY + height; y++)
                {
                    if (x >= 0 && x < GridManager.Instance.gridWidth && y >= 0 && y < GridManager.Instance.gridHeight)
                    {
                        if (_fogObjects[x, y] != null)
                        {
                            _fogObjects[x, y].SetActive(false);
                        }
                    }
                }
            }
            
            Debug.Log($"[FogManager] Đã mở khóa khu vực {startX},{startY} (Kích thước {width}x{height})");
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
                if (hit.collider != null && hit.collider.gameObject.transform.parent == fogParent)
                {
                    Vector2Int gridPos = GridManager.Instance.GetGridPosition(hit.collider.transform.position);
                    
                    TryUnlockArea(gridPos.x, gridPos.y, 4, 4);
                }
            }
        }
    }
}
