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

            itemsContainer = new GameObject("ItemsLayer");
            itemsContainer.transform.SetParent(transform);
        }

        private void SetupTilemaps()
        {
            GameObject gridObj = new GameObject("RuntimeGrid");
            gridObj.transform.SetParent(transform);
            grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(1f, 1f, 0); // Sử dụng 1x1 cho lưới vuông
            grid.cellLayout = GridLayout.CellLayout.Rectangle;
            

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

            BaseGridManager.Instance.LoadTerrainData(mapData.terrainData, mapData.gridWidth, mapData.gridHeight);

            if (mappingConfig == null)
            {
                Debug.LogWarning("[RuntimeMapRenderer] ChÆ°a gÃ¡n TileToIDMapping Config, khÃ´ng thá»ƒ load hÃ¬nh áº£nh Ä‘áº¥t!");
            }

            if (mapData.groundLayers != null)
            {
                for (int i = 0; i < mapData.groundLayers.Count; i++)
                {
                    var layerData = mapData.groundLayers[i];
                    
                    GameObject groundObj = new GameObject($"GroundTilemap_{layerData.layerName}");
                    groundObj.transform.SetParent(grid.transform);
                    Tilemap tMap = groundObj.AddComponent<Tilemap>();
                    var tRender = groundObj.AddComponent<TilemapRenderer>();
                    tRender.sortOrder = TilemapRenderer.SortOrder.TopRight;
                    tRender.sortingOrder = -100 + i; // Layer cÃ ng sau thÃ¬ render cÃ ng cao (-100, -99, -98...)
                    
                    groundTilemaps.Add(tMap);

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
                                        if (x == 0 && y == 0)
                                        {
                                            Debug.Log($"[RuntimeMapRenderer] Đặt gạch đầu tiên thành công: {gID} tại (0,0)");
                                        }
                                    }
                                    else
                                    {
                                        Debug.LogWarning($"[RuntimeMapRenderer] Không tìm thấy TileBase cho ID gạch: {gID}");
                                    }
                                }
                            }
                        }
                    }
                }
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
