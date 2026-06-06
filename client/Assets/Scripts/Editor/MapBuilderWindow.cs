using System.IO;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using GameClient.Gameplay.BaseBuilder;
using GameClient.Gameplay.Items;
using System.Net.Http;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Grpc.Core;
using System.Collections.Generic;

namespace GameClient.EditorTools
{
    public class MapBuilderWindow : EditorWindow
    {
        [Header("Scene Tilemaps")]
        public Tilemap terrainTilemap; // Dùng để check va chạm (Unbuildable)
        public List<Tilemap> groundTilemaps; // Danh sách các lớp đất
        public Tilemap fogTilemap;
        public Tilemap itemTilemap;
        public Tilemap buildingTilemap;

        [Header("Scene GameObjects Container")]
        [Tooltip("Kéo thả GameObject cha chứa tất cả các nhà và item thiết kế trong Scene vào đây.")]
        public Transform objectsRoot;

        [Header("Snapping Behavior")]
        public bool autoSnapInScene = true;

        private TileToIDMapping mappingConfig;
        private int mapWidth = 32;
        private int mapHeight = 32;

        private const string MOCK_MAP_FILE = "Assets/StreamingAssets/DefaultMap.json";
        private Vector2 scrollPosition;
        private bool showPalette = true;

        [MenuItem("Tools/Map Builder")]
        public static void ShowWindow()
        {
            GetWindow<MapBuilderWindow>("Map Builder");
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private float GetTileWidth()
        {
            if (terrainTilemap != null && terrainTilemap.layoutGrid != null)
                return terrainTilemap.layoutGrid.cellSize.x;
            Grid g = FindFirstObjectByType<Grid>();
            return g != null ? g.cellSize.x : 1f;
        }

        private float GetTileHeight()
        {
            if (terrainTilemap != null && terrainTilemap.layoutGrid != null)
                return terrainTilemap.layoutGrid.cellSize.y;
            Grid g = FindFirstObjectByType<Grid>();
            return g != null ? g.cellSize.y : 1f;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (terrainTilemap == null || terrainTilemap.layoutGrid == null) return;

            Grid grid = terrainTilemap.layoutGrid;
            Vector3 p00 = grid.CellToWorld(new Vector3Int(0, 0, 0));
            Vector3 p10 = grid.CellToWorld(new Vector3Int(mapWidth, 0, 0));
            Vector3 p01 = grid.CellToWorld(new Vector3Int(0, mapHeight, 0));
            Vector3 p11 = grid.CellToWorld(new Vector3Int(mapWidth, mapHeight, 0));

            Handles.color = Color.green;
            Handles.DrawLine(p00, p10, 3f);
            Handles.DrawLine(p10, p11, 3f);
            Handles.DrawLine(p11, p01, 3f);
            Handles.DrawLine(p01, p00, 3f);

            GUIStyle style = new GUIStyle();
            style.normal.textColor = Color.green;
            style.fontSize = 20;
            style.fontStyle = FontStyle.Bold;
            Handles.Label(p00, " (0, 0)", style);
            Handles.Label(p11, $" ({mapWidth}, {mapHeight})", style);
            
            if (autoSnapInScene && objectsRoot != null && Selection.transforms != null)
            {
                float tileWidth = GetTileWidth();
                float tileHeight = GetTileHeight();

                foreach (Transform t in Selection.transforms)
                {
                    if (t.IsChildOf(objectsRoot) && t != objectsRoot)
                    {
                        string cleanName = t.name.Split(' ')[0].Split('(')[0].Trim();
                        bool isBuilding = IsBuildingID(cleanName);
                        int sizeX = 1;
                        int sizeY = 1;

                        if (isBuilding)
                        {
                            var bData = Resources.Load<BuildingData>($"GameData/Buildings/{cleanName}");
                            if (bData != null)
                            {
                                sizeX = bData.SizeX;
                                sizeY = bData.SizeY;
                            }
                        }

                        float worldX = t.position.x;
                        float worldY = t.position.y;

                        int gx = Mathf.RoundToInt(worldX / tileWidth - sizeX / 2f);
                        int gy = Mathf.RoundToInt(worldY / tileHeight - sizeY / 2f);

                        float snapX = (gx + sizeX / 2f) * tileWidth;
                        float snapY = (gy + sizeY / 2f) * tileHeight;

                        Vector3 targetPos = new Vector3(snapX, snapY, t.position.z);
                        if (Vector3.Distance(t.position, targetPos) > 0.01f)
                        {
                            Undo.RecordObject(t, "Auto Snap to Grid");
                            t.position = targetPos;
                        }
                    }
                }
            }

            HandleUtility.Repaint();
        }

        private void OnGUI()
        {
            GUILayout.Label("Interactive Map Builder", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Sử dụng Container duy nhất để chứa cả nhà và item. Bạn có thể bấm thêm nhanh vật thể từ thư viện và kéo thả trực tiếp, đối tượng sẽ tự động căn theo ô lưới.", MessageType.Info);

            GUILayout.Space(10);
            mappingConfig = (TileToIDMapping)EditorGUILayout.ObjectField("Tile Mapping Config", mappingConfig, typeof(TileToIDMapping), false);
            
            GUILayout.Space(5);
            GUILayout.Label("Thiết kế bằng GameObjects (Khuyên dùng)", EditorStyles.boldLabel);
            objectsRoot = (Transform)EditorGUILayout.ObjectField("Objects Container Root", objectsRoot, typeof(Transform), true);
            autoSnapInScene = EditorGUILayout.Toggle("Auto Snap in Scene (Tự hút lưới)", autoSnapInScene);

            if (GUILayout.Button("Căn lưới thủ công (Snap All)", GUILayout.Height(30)))
            {
                SnapObjectsToGrid();
            }

            GUILayout.Space(10);
            DrawPalette();

            GUILayout.Space(15);
            GUILayout.Label("Thiết kế bằng Tilemap (Dành cho Lớp Đất / Sương mù)", EditorStyles.boldLabel);
            terrainTilemap = (Tilemap)EditorGUILayout.ObjectField("Terrain Tilemap", terrainTilemap, typeof(Tilemap), true);
            fogTilemap = (Tilemap)EditorGUILayout.ObjectField("Fog Tilemap", fogTilemap, typeof(Tilemap), true);
            buildingTilemap = (Tilemap)EditorGUILayout.ObjectField("Building Tilemap (Dự phòng)", buildingTilemap, typeof(Tilemap), true);
            itemTilemap = (Tilemap)EditorGUILayout.ObjectField("Item Tilemap (Dự phòng)", itemTilemap, typeof(Tilemap), true);

            SerializedObject so = new SerializedObject(this);
            SerializedProperty groundProp = so.FindProperty("groundTilemaps");
            EditorGUILayout.PropertyField(groundProp, new GUIContent("Ground Tilemaps"), true);
            so.ApplyModifiedProperties();

            GUILayout.Space(10);
            mapWidth = EditorGUILayout.IntField("Map Width", mapWidth);
            mapHeight = EditorGUILayout.IntField("Map Height", mapHeight);

            GUILayout.Space(20);
            if (GUILayout.Button("Export Map To JSON (Save Locally)", GUILayout.Height(40)))
            {
                ExportMapLocally();
            }

            if (GUILayout.Button("Export Map & Save To Server", GUILayout.Height(40)))
            {
                ExportMapToServer();
            }
        }

        private void DrawPalette()
        {
            showPalette = EditorGUILayout.Foldout(showPalette, "Thư viện Object để thêm nhanh", true);
            if (!showPalette) return;

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(180));
            GUILayout.Label("Buildings (Nhấp [+] để đặt vào Scene):", EditorStyles.boldLabel);
            
            var allBuildingData = Resources.LoadAll<BuildingData>("GameData/Buildings");
            foreach (var bData in allBuildingData)
            {
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                GUILayout.Label($"{bData.BuildingID} ({bData.SizeX}x{bData.SizeY})", GUILayout.Width(200));
                if (GUILayout.Button("+ Đặt vào Scene", GUILayout.Width(120)))
                {
                    PlaceObjectInScene(bData.BuildingID, true);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(5);
            GUILayout.Label("Items / Trang trí:", EditorStyles.boldLabel);
            if (mappingConfig != null)
            {
                foreach (var iMap in mappingConfig.itemMappings)
                {
                    GUILayout.BeginHorizontal(EditorStyles.helpBox);
                    GUILayout.Label($"{iMap.entityID} (1x1)", GUILayout.Width(200));
                    if (GUILayout.Button("+ Đặt vào Scene", GUILayout.Width(120)))
                    {
                        PlaceObjectInScene(iMap.entityID, false);
                    }
                    GUILayout.EndHorizontal();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Hãy gán Tile Mapping Config để tải danh sách Item.", MessageType.Warning);
            }

            GUILayout.EndScrollView();
        }

        private void PlaceObjectInScene(string entityID, bool isBuilding)
        {
            if (objectsRoot == null)
            {
                GameObject rootGo = GameObject.Find("MapObjects");
                if (rootGo == null) rootGo = new GameObject("MapObjects");
                objectsRoot = rootGo.transform;
            }

            GameObject instance = null;

            if (isBuilding)
            {
                var bData = Resources.Load<BuildingData>($"GameData/Buildings/{entityID}");
                if (bData != null && !string.IsNullOrEmpty(bData.PrefabAddress))
                {
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(bData.PrefabAddress);
                    if (prefab != null)
                    {
                        instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    }
                }
            }

            if (instance == null)
            {
                instance = new GameObject(entityID);
                var sr = instance.AddComponent<SpriteRenderer>();
                
                if (isBuilding)
                {
                    var bData = Resources.Load<BuildingData>($"GameData/Buildings/{entityID}");
                    if (bData != null && bData.VisualConfig != null)
                    {
                        var vis = bData.VisualConfig.GetVisualsForLevel(1);
                        if (vis != null && vis.normalSprite != null)
                        {
                            sr.sprite = vis.normalSprite;
                        }
                    }
                    
                    int sizeX = bData != null ? bData.SizeX : 1;
                    int sizeY = bData != null ? bData.SizeY : 1;
                    var col = instance.AddComponent<BoxCollider2D>();
                    col.size = new Vector2(sizeX, sizeY);
                }
                else
                {
                    if (mappingConfig != null)
                    {
                        TileBase iTile = mappingConfig.GetItemTile(entityID);
                        if (iTile != null && iTile is Tile t)
                        {
                            sr.sprite = t.sprite;
                        }
                    }
                    var col = instance.AddComponent<BoxCollider2D>();
                    col.size = new Vector2(1, 1);
                }
            }

            instance.transform.SetParent(objectsRoot);
            instance.name = entityID; // Giữ tên sạch để dễ parse ID khi Export

            Vector3 spawnPos = Vector3.zero;
            if (SceneView.lastActiveSceneView != null)
            {
                spawnPos = SceneView.lastActiveSceneView.pivot;
                spawnPos.z = 0;
            }
            instance.transform.position = spawnPos;

            int snapSizeX = 1;
            int snapSizeY = 1;
            if (isBuilding)
            {
                var bData = Resources.Load<BuildingData>($"GameData/Buildings/{entityID}");
                if (bData != null)
                {
                    snapSizeX = bData.SizeX;
                    snapSizeY = bData.SizeY;
                }
            }

            float tileWidth = GetTileWidth();
            float tileHeight = GetTileHeight();

            int gx = Mathf.RoundToInt(spawnPos.x / tileWidth - snapSizeX / 2f);
            int gy = Mathf.RoundToInt(spawnPos.y / tileHeight - snapSizeY / 2f);

            float snapX = (gx + snapSizeX / 2f) * tileWidth;
            float snapY = (gy + snapSizeY / 2f) * tileHeight;
            instance.transform.position = new Vector3(snapX, snapY, 0);

            Selection.activeGameObject = instance;
            Undo.RegisterCreatedObjectUndo(instance, "Place Object");
            Debug.Log($"<color=green>[MapBuilder]</color> Đã tạo visual cho {entityID} tại toạ độ lưới ({gx}, {gy})");
        }

        private void SnapObjectsToGrid()
        {
            if (objectsRoot == null) return;
            int snappedCount = 0;
            float tileWidth = GetTileWidth();
            float tileHeight = GetTileHeight();

            foreach (Transform child in objectsRoot)
            {
                if (!child.gameObject.activeSelf) continue;

                string cleanName = child.name.Split(' ')[0].Split('(')[0].Trim();
                bool isBuilding = IsBuildingID(cleanName);
                int sizeX = 1;
                int sizeY = 1;

                if (isBuilding)
                {
                    var bData = Resources.Load<BuildingData>($"GameData/Buildings/{cleanName}");
                    if (bData != null)
                    {
                        sizeX = bData.SizeX;
                        sizeY = bData.SizeY;
                    }
                }

                float worldX = child.position.x;
                float worldY = child.position.y;

                int gx = Mathf.RoundToInt(worldX / tileWidth - sizeX / 2f);
                int gy = Mathf.RoundToInt(worldY / tileHeight - sizeY / 2f);

                float snapX = (gx + sizeX / 2f) * tileWidth;
                float snapY = (gy + sizeY / 2f) * tileHeight;

                Undo.RecordObject(child, "Snap to Grid");
                child.position = new Vector3(snapX, snapY, child.position.z);
                snappedCount++;
            }
            Debug.Log($"<color=green>[MapBuilder]</color> Đã căn lưới thành công cho {snappedCount} đối tượng.");
        }

        private bool IsBuildingID(string id)
        {
            if (mappingConfig != null)
            {
                var match = mappingConfig.buildingMappings.Find(x => x.entityID == id);
                if (match != null) return true;
            }
            var bData = Resources.Load<BuildingData>($"GameData/Buildings/{id}");
            return bData != null;
        }

        private void ExportMapLocally()
        {
            if (mappingConfig == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng gán file TileToIDMapping Config!", "OK");
                return;
            }

            var mapData = CreateExportModel();
            string json = JsonUtility.ToJson(mapData, true);
            File.WriteAllText(MOCK_MAP_FILE, json);
            AssetDatabase.Refresh();
            Debug.Log($"<color=green>[MapBuilder]</color> Xuất thành công DefaultMap.json! (Buildings: {mapData.buildings.Count}, Items: {mapData.items.Count})");
        }

        private async void ExportMapToServer()
        {
            if (mappingConfig == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Vui lòng gán file TileToIDMapping Config!", "OK");
                return;
            }

            var mapData = CreateExportModel();
            string json = JsonUtility.ToJson(mapData, true);
            File.WriteAllText(MOCK_MAP_FILE, json);
            AssetDatabase.Refresh();

            try
            {
                var handler = new HttpClientHandler();
                handler.UseProxy = false;
                var httpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWebText, handler);

                string host = GameClient.Core.GameConstants.Network.GATEWAY_HOST;
                int port = GameClient.Core.GameConstants.Network.GATEWAY_PORT;
                var channel = GrpcChannel.ForAddress($"http://{host}:{port}", new GrpcChannelOptions {
                    HttpHandler = httpHandler,
                    Credentials = ChannelCredentials.Insecure
                });

                var client = new GameClient.Network.Pb.GatewayService.GatewayServiceClient(channel);
                var req = new GameClient.Network.Pb.SaveAdminMapRequest { MapJsonData = json };
                var res = await client.SaveAdminMapAsync(req);

                if (res.Base.Code == 0)
                {
                    Debug.Log($"<color=green>[MapBuilder]</color> Đã lưu map lên Server thành công!");
                    EditorUtility.DisplayDialog("Thành công", "Đã lưu map lên Server!", "OK");
                }
                else
                {
                    Debug.LogError($"[MapBuilder] Server báo lỗi: {res.Base.Message}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MapBuilder] Lỗi kết nối Server: {ex.Message}");
            }
        }

        private BaseExportModel CreateExportModel()
        {
            var mapData = new BaseExportModel
            {
                gridWidth = mapWidth,
                gridHeight = mapHeight,
                terrainData = new int[mapWidth * mapHeight],
                fogData = new int[mapWidth * mapHeight]
            };

            if (groundTilemaps != null)
            {
                foreach (var gt in groundTilemaps)
                {
                    if (gt != null)
                    {
                        mapData.groundLayers.Add(new ExportedGroundLayer
                        {
                            layerName = gt.name,
                            tiles = new string[mapWidth * mapHeight]
                        });
                    }
                }
            }

            float tileWidth = GetTileWidth();
            float tileHeight = GetTileHeight();

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    Vector3Int cellPos = new Vector3Int(x, y, 0);
                    int flatIndex = y * mapWidth + x;

                    if (terrainTilemap != null)
                    {
                        var tTile = terrainTilemap.GetTile(cellPos);
                        if (tTile != null)
                        {
                            if (mappingConfig.IsUnbuildable(tTile))
                            {
                                mapData.terrainData[flatIndex] = 1;
                            }
                        }
                    }

                    if (groundTilemaps != null)
                    {
                        for (int i = 0; i < groundTilemaps.Count; i++)
                        {
                            var gt = groundTilemaps[i];
                            if (gt != null)
                            {
                                var gTile = gt.GetTile(cellPos);
                                if (gTile != null)
                                {
                                    string gID = mappingConfig.GetGroundID(gTile);
                                    if (!string.IsNullOrEmpty(gID))
                                    {
                                        mapData.groundLayers[i].tiles[flatIndex] = gID;
                                    }
                                }
                            }
                        }
                    }

                    if (fogTilemap != null)
                    {
                        var fTile = fogTilemap.GetTile(cellPos);
                        if (fTile != null)
                        {
                            mapData.fogData[flatIndex] = 1;
                        }
                    }

                    if (objectsRoot == null && buildingTilemap != null)
                    {
                        var bTile = buildingTilemap.GetTile(cellPos);
                        if (bTile != null)
                        {
                            string bID = mappingConfig.GetBuildingID(bTile);
                            if (!string.IsNullOrEmpty(bID))
                            {
                                Matrix4x4 matrix = buildingTilemap.GetTransformMatrix(cellPos);
                                bool isFlipped = matrix.m00 < 0;
                                BuildingState bState = mappingConfig.GetBuildingState(bTile);
                                int bLevel = mappingConfig.GetBuildingLevel(bTile);

                                mapData.buildings.Add(new ExportedBuilding
                                {
                                    id = bID,
                                    x = x,
                                    y = y,
                                    flipX = isFlipped,
                                    level = bLevel,
                                    state = bState,
                                    currentHP = 1000
                                });
                            }
                        }
                    }

                    if (objectsRoot == null && itemTilemap != null)
                    {
                        var iTile = itemTilemap.GetTile(cellPos);
                        if (iTile != null)
                        {
                            string iID = mappingConfig.GetItemID(iTile);
                            if (!string.IsNullOrEmpty(iID))
                            {
                                Matrix4x4 matrix = itemTilemap.GetTransformMatrix(cellPos);
                                bool isFlipped = matrix.m00 < 0;

                                mapData.items.Add(new ExportedItem
                                {
                                    id = iID,
                                    x = x,
                                    y = y,
                                    flipX = isFlipped,
                                    quantity = 1
                                });
                            }
                        }
                    }
                }
            }

            if (objectsRoot != null)
            {
                foreach (Transform child in objectsRoot)
                {
                    if (!child.gameObject.activeSelf) continue;

                    string cleanName = child.name.Split(' ')[0].Split('(')[0].Trim();
                    bool isBuilding = IsBuildingID(cleanName);

                    bool isFlipped = false;
                    var sr = child.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null)
                    {
                        isFlipped = sr.flipX;
                    }
                    if (child.localScale.x < 0)
                    {
                        isFlipped = !isFlipped;
                    }

                    if (isBuilding)
                    {
                        int sizeX = 1;
                        int sizeY = 1;
                        int bLevel = 1;
                        BuildingState bState = BuildingState.Normal;

                        var instance = child.GetComponent<BuildingInstance>();
                        if (instance != null)
                        {
                            if (instance.Data != null)
                            {
                                sizeX = instance.Data.SizeX;
                                sizeY = instance.Data.SizeY;
                            }
                            bLevel = instance.CurrentLevel;
                            bState = instance.CurrentState;
                            isFlipped = instance.FlipX;
                        }
                        else
                        {
                            var bData = Resources.Load<BuildingData>($"GameData/Buildings/{cleanName}");
                            if (bData != null)
                            {
                                sizeX = bData.SizeX;
                                sizeY = bData.SizeY;
                            }
                        }

                        float worldX = child.position.x;
                        float worldY = child.position.y;

                        int gx = Mathf.RoundToInt(worldX / tileWidth - sizeX / 2f);
                        int gy = Mathf.RoundToInt(worldY / tileHeight - sizeY / 2f);

                        mapData.buildings.Add(new ExportedBuilding
                        {
                            id = cleanName,
                            x = gx,
                            y = gy,
                            flipX = isFlipped,
                            level = bLevel,
                            state = bState,
                            currentHP = 1000
                        });
                    }
                    else
                    {
                        float worldX = child.position.x;
                        float worldY = child.position.y;

                        int gx = Mathf.RoundToInt(worldX / tileWidth);
                        int gy = Mathf.RoundToInt(worldY / tileHeight);

                        mapData.items.Add(new ExportedItem
                        {
                            id = cleanName,
                            x = gx,
                            y = gy,
                            flipX = isFlipped,
                            quantity = 1
                        });
                    }
                }
            }

            return mapData;
        }
    }
}
