using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using GameClient.Managers;
using GameClient.Gameplay.Items;

namespace GameClient.Gameplay.BaseBuilder
{
    public class RuntimeMapRenderer : MonoBehaviour
    {
        public static RuntimeMapRenderer Instance { get; private set; }

        [Header("Mapping Config")]
        public TileToIDMapping mappingConfig; // GÃ¡n tá»« Inspector

        private Grid grid;
        private List<Tilemap> groundTilemaps = new List<Tilemap>();
        private Tilemap fogTilemap;

        private GameObject itemsContainer;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            SetupTilemaps();

            // Fallback: nếu mappingConfig chưa được gán trong Inspector (hoặc bị mất ref trong build)
            // thì tự động load từ Resources
            if (mappingConfig == null)
            {
                mappingConfig = Resources.Load<TileToIDMapping>("GameData/MainTileMapping");
                if (mappingConfig != null)
                {
                    Debug.Log("[RuntimeMapRenderer] Đã tự động load TileToIDMapping từ Resources/GameData/MainTileMapping");
                }
                else
                {
                    Debug.LogError("[RuntimeMapRenderer] Không tìm thấy TileToIDMapping! Map nền sẽ không hiển thị.");
                }
            }

            itemsContainer = new GameObject("ItemsLayer");
            itemsContainer.transform.SetParent(transform);
        }

        private void SetupTilemaps()
        {
            GameObject gridObj = new GameObject("RuntimeGrid");
            gridObj.transform.SetParent(transform);
            grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 0.5f, 0); 
            grid.cellLayout = GridLayout.CellLayout.Isometric;
            

            GameObject fogObj = new GameObject("FogTilemap");
            fogObj.transform.SetParent(gridObj.transform);
            fogTilemap = fogObj.AddComponent<Tilemap>();
            var fogRenderer = fogObj.AddComponent<TilemapRenderer>();
            fogRenderer.sortOrder = TilemapRenderer.SortOrder.TopRight;
            fogRenderer.sortingOrder = 30000; // Náº±m trÃªn cÃ¹ng che khuáº¥t táº§m nhÃ¬n
            
            fogTilemap.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        }

        public void RenderMapLayers(BaseExportModel mapData)
        {
            ClearOldLayers();

            if (mapData == null) return;

            Debug.Log($"[RuntimeMapRenderer][DEBUG] RenderMapLayers bắt đầu: gridW={mapData.gridWidth} gridH={mapData.gridHeight} groundLayers={mapData.groundLayers?.Count ?? 0} mappingConfig={mappingConfig != null}");

            BaseGridManager.Instance.LoadTerrainData(mapData.terrainData, mapData.gridWidth, mapData.gridHeight);

            if (mappingConfig == null)
            {
                Debug.LogError("[RuntimeMapRenderer][DEBUG] mappingConfig == NULL! Không thể render ground tiles. Kiểm tra Inspector hoặc Resources/GameData/MainTileMapping.");
                return;
            }

            if (mapData.groundLayers == null || mapData.groundLayers.Count == 0)
            {
                if (mapData.groundTiles != null && mapData.groundTiles.Length > 0)
                {
                    Debug.Log($"[RuntimeMapRenderer][DEBUG] groundLayers rỗng nhưng groundTiles có {mapData.groundTiles.Length} tiles. Tạo layer ảo từ groundTiles.");
                    mapData.groundLayers = new List<ExportedGroundLayer>
                    {
                        new ExportedGroundLayer
                        {
                            layerName = "DefaultGround",
                            tiles = mapData.groundTiles
                        }
                    };
                }
                else
                {
                    Debug.LogError("[RuntimeMapRenderer][DEBUG] Cả groundLayers và groundTiles đều rỗng/null — không có gì để render!");
                    return;
                }
            }

            for (int i = 0; i < mapData.groundLayers.Count; i++)
            {
                var layerData = mapData.groundLayers[i];

                int totalTiles = layerData.tiles?.Length ?? 0;
                int nonNullTiles = 0;
                if (layerData.tiles != null)
                    foreach (var t in layerData.tiles) if (!string.IsNullOrEmpty(t)) nonNullTiles++;

                Debug.Log($"[RuntimeMapRenderer][DEBUG] Layer [{i}] '{layerData.layerName}': tiles.Length={totalTiles} nonEmpty={nonNullTiles}");

                GameObject groundObj = new GameObject($"GroundTilemap_{layerData.layerName}");
                groundObj.transform.SetParent(grid.transform);
                Tilemap tMap = groundObj.AddComponent<Tilemap>();
                var tRender = groundObj.AddComponent<TilemapRenderer>();
                tRender.sortOrder = TilemapRenderer.SortOrder.TopRight;
                tRender.sortingOrder = -100 + i;
                
                // Sử dụng Sprite-Default shader để tránh bị tối/không hiển thị khi thiếu Light 2D
                var defaultMaterial = Shader.Find("Sprites/Default") != null ? new Material(Shader.Find("Sprites/Default")) : null;
                if (defaultMaterial != null)
                {
                    tRender.material = defaultMaterial;
                }

                groundTilemaps.Add(tMap);

                int placedCount = 0;
                int missingTileCount = 0;

                for (int x = 0; x < mapData.gridWidth; x++)
                {
                    for (int y = 0; y < mapData.gridHeight; y++)
                    {
                        int idx = y * mapData.gridWidth + x;
                        Vector3Int cellPos = new Vector3Int(x, y, 0);

                        if (layerData.tiles != null && idx < layerData.tiles.Length)
                        {
                            string gID = layerData.tiles[idx];
                            if (!string.IsNullOrEmpty(gID))
                            {
                                TileBase gTile = mappingConfig.GetGroundTile(gID);
                                if (gTile != null)
                                {
                                    tMap.SetTile(cellPos, gTile);
                                    placedCount++;
                                    if (x == 0 && y == 0)
                                    {
                                        Debug.Log($"[RuntimeMapRenderer] Đặt gạch đầu tiên thành công: {gID} tại (0,0)");
                                    }
                                }
                                else
                                {
                                    missingTileCount++;
                                    if (missingTileCount <= 3)
                                        Debug.LogWarning($"[RuntimeMapRenderer][DEBUG] GetGroundTile('{gID}') trả về NULL — ID này không có trong mappingConfig!");
                                }
                            }
                        }
                    }
                }

                Debug.Log($"[RuntimeMapRenderer][DEBUG] Layer [{i}] '{layerData.layerName}': đặt được {placedCount} tiles, thiếu mapping {missingTileCount} tiles");
            }

            if (mapData.items != null)
            {
                foreach (var item in mapData.items)
                {
                    CreateItemInstance(item);
                }
            }
        }

        private void CreateItemInstance(ExportedItem itemData)
        {
            GameObject itemObj = new GameObject($"Item_{itemData.id}_{itemData.x}_{itemData.y}");
            itemObj.transform.SetParent(itemsContainer.transform);
            
            Vector3 worldPos = BaseGridManager.Instance.GridToWorldPosition(itemData.x, itemData.y);
            worldPos.x += BaseGridManager.TILE_WIDTH / 2f;
            worldPos.y += BaseGridManager.TILE_HEIGHT / 2f;
            itemObj.transform.position = worldPos;

            SpriteRenderer sr = itemObj.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10 + (1000 - itemData.y); // Isometric sorting

            if (mappingConfig != null)
            {
                TileBase iTile = mappingConfig.GetItemTile(itemData.id);
                if (iTile != null && iTile is Tile t)
                {
                    sr.sprite = t.sprite; // Láº¥y tháº³ng sprite tá»« TileBase Ä‘á»ƒ hiá»ƒn thá»‹ Runtime
                }
            }

            var collider = itemObj.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1, 1);
            
            var instance = itemObj.AddComponent<ItemInstance>();
            instance.Setup(itemData);
        }

        private void ClearOldLayers()
        {
            foreach (var gt in groundTilemaps)
            {
                if (gt != null) Destroy(gt.gameObject);
            }
            groundTilemaps.Clear();
            
            fogTilemap.ClearAllTiles();
            
            foreach (Transform child in itemsContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}
