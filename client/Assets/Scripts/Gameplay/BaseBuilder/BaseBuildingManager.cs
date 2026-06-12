using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using GameClient.Core;
using GameClient.Managers;
using GameClient.Network;

namespace GameClient.Gameplay.BaseBuilder
{
    public class BaseBuildingManager : Singleton<BaseBuildingManager>
    {
        private List<BuildingInstance> _activeBuildings = new List<BuildingInstance>();

        private Dictionary<string, BuildingData> _buildingDatabase = new Dictionary<string, BuildingData>();

        public Dictionary<string, BuildingData> GetBuildingDatabase() => _buildingDatabase;

        public BuildingInstance GetFirstBuilding(
            string buildingId)
        {
            return _activeBuildings.Find(b => b.Data != null && b.Data.BuildingID == buildingId);
        }

        public int GetBuildingCount(string buildingId)
        {
            int count = 0;
            foreach (var b in _activeBuildings)
            {
                if (b != null && b.Data != null && b.Data.BuildingID == buildingId)
                {
                    count++;
                }
            }
            return count;
        }

        public BuildingInstance GetBuildingAt(int gridX, int gridY)
        {
            return _activeBuildings.Find(b => 
                b != null && b.Data != null &&
                gridX >= b.GridX && gridX < b.GridX + b.Data.SizeX &&
                gridY >= b.GridY && gridY < b.GridY + b.Data.SizeY);
        }

        protected override void Awake()
        {
            base.Awake();
            var allData = Resources.LoadAll<BuildingData>("GameData/Buildings");
            foreach (var data in allData)
            {
                if (!_buildingDatabase.ContainsKey(data.BuildingID))
                {
                    _buildingDatabase.Add(data.BuildingID, data);
                }
            }
        }

        public void ResetManager()
        {
            foreach (var b in _activeBuildings)
            {
                if (b != null)
                {
                    Destroy(b.gameObject);
                }
            }
            _activeBuildings.Clear();
        }

        public void LoadDatabase(List<BuildingData> dataList)
        {
            _buildingDatabase.Clear();
            foreach (var data in dataList)
            {
                if (!_buildingDatabase.ContainsKey(data.BuildingID))
                {
                    _buildingDatabase.Add(data.BuildingID, data);
                }
            }
        }

        public async Task<bool> PlaceBuilding(string buildingId, int gridX, int gridY, int level = 1, BuildingState state = BuildingState.Normal, bool flipX = false, long instanceId = 0)
        {
            if (!_buildingDatabase.TryGetValue(buildingId, out BuildingData data))
            {
                Debug.LogWarning($"[BaseBuilding] Không tìm thấy dữ liệu nhà ID: {buildingId}. Sẽ dùng Mock Data.");
                data = ScriptableObject.CreateInstance<BuildingData>();
                data.BuildingID = buildingId;
                
                var visualConfig = Resources.Load<BuildingVisualConfig>($"GameData/Buildings/{buildingId}_Visuals");
                if (visualConfig != null)
                {
                    data.VisualConfig = visualConfig;
                }
            }

            if (!BaseGridManager.Instance.IsSpaceAvailable(gridX, gridY, data.SizeX, data.SizeY))
            {
                Debug.LogWarning($"[BaseBuilding] Không thể đặt {buildingId} tại ({gridX}, {gridY}). Kẹt rồi!");
                return false; // Hủy đặt
            }

            BaseGridManager.Instance.SetOccupied(gridX, gridY, data.SizeX, data.SizeY, true);

            GameObject go = null;
            if (!string.IsNullOrEmpty(data.PrefabAddress))
            {
                go = await ResourceManager.Instance.InstantiateAsync(data.PrefabAddress);
            }
            
            if (go == null)
            {
                go = new GameObject("Building_" + buildingId);
                
                var oldCol = go.GetComponent<BoxCollider2D>();
                if (oldCol != null) Destroy(oldCol);

                var collider = go.GetComponent<PolygonCollider2D>();
                if (collider == null)
                {
                    collider = go.AddComponent<PolygonCollider2D>();
                }
                float w = data.SizeX * BaseGridManager.TILE_WIDTH;
                float h = data.SizeY * BaseGridManager.TILE_HEIGHT;
                Vector2[] points = new Vector2[4];
                points[0] = new Vector2(0, h / 2f);
                points[1] = new Vector2(w / 2f, 0);
                points[2] = new Vector2(0, -h / 2f);
                points[3] = new Vector2(-w / 2f, 0);
                collider.points = points;
                
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 10000;

                if (data.VisualConfig == null)
                {
                    var mockObj = new GameObject("MockVisual");
                    mockObj.transform.SetParent(go.transform, false);
                    var mockSr = mockObj.AddComponent<SpriteRenderer>();
                    
                    Texture2D tex = new Texture2D(1, 1);
                    tex.SetPixel(0, 0, new Color(1f, 0.5f, 0f, 0.8f)); // Màu cam trong suốt
                    tex.Apply();
                    
                    mockSr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                    mockSr.sortingOrder = 9999;
                    mockObj.transform.localScale = new Vector3(data.SizeX * BaseGridManager.TILE_WIDTH, data.SizeY * BaseGridManager.TILE_HEIGHT, 1f);
                    
                    Debug.LogWarning($"[BaseBuilding] Dùng hình ảnh giữ chỗ (Mock) cho {buildingId} vì không có VisualConfig!");
                }
            }
            
            BuildingInstance instance = go.AddComponent<BuildingInstance>();
            instance.Setup(data, gridX, gridY, level, state, flipX, instanceId);
            
            _activeBuildings.Add(instance);
            
            Debug.Log($"[BaseBuilding] Đã xây {data.BuildingNameKey} tại ({gridX}, {gridY})");
            return true;
        }

        public void RemoveBuilding(BuildingInstance building)
        {
            if (building == null) return;
            
            BaseGridManager.Instance.SetOccupied(building.GridX, building.GridY, building.Data.SizeX, building.Data.SizeY, false);
            
            _activeBuildings.Remove(building);
            Destroy(building.gameObject);
        }

        public void ClearAllBuildings()
        {
            foreach (var b in _activeBuildings)
            {
                if (b != null)
                {
                    BaseGridManager.Instance.SetOccupied(b.GridX, b.GridY, b.Data.SizeX, b.Data.SizeY, false);
                    Destroy(b.gameObject);
                }
            }
            _activeBuildings.Clear();
        }

        #region HỆ THỐNG EXPORT / IMPORT CHIA SẺ CODE BASE

        public string ExportLayoutToBase64()
        {
            BaseExportModel model = new BaseExportModel
            {
                gridWidth = BaseGridManager.Instance.Width,
                gridHeight = BaseGridManager.Instance.Height
            };

            foreach (var b in _activeBuildings)
            {
                model.buildings.Add(new ExportedBuilding
                {
                    instance_id = b.InstanceID,
                    id = b.Data.BuildingID,
                    x = b.GridX,
                    y = b.GridY
                });
            }

            string json = JsonUtility.ToJson(model);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(json);
            string base64 = System.Convert.ToBase64String(bytes);

            Debug.Log($"[BaseBuilding] Đã Export mã Layout: {base64}");
            return base64;
        }

        public async Task ImportLayoutFromBase64(string base64Code)
        {
            try
            {
                byte[] bytes = System.Convert.FromBase64String(base64Code);
                string json = System.Text.Encoding.UTF8.GetString(bytes);

                BaseExportModel model = JsonUtility.FromJson<BaseExportModel>(json);
                await ImportLayoutFromModel(model);
                Debug.Log($"[BaseBuilding] Import thành công từ Base64!");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BaseBuilding] Import lỗi! Mã Code không hợp lệ. Khúc mắc: {ex.Message}");
            }
        }
        
        public async Task ImportLayoutFromModel(BaseExportModel model)
        {
            if (model == null) return;

            if (model.gridWidth > BaseGridManager.Instance.Width || model.gridHeight > BaseGridManager.Instance.Height)
            {
                BaseGridManager.Instance.ExpandGrid(model.gridWidth, model.gridHeight);
            }

            // Chỉ render map layers nếu model có chứa thông tin groundLayers hoặc groundTiles
            if ((model.groundLayers != null && model.groundLayers.Count > 0) || (model.groundTiles != null && model.groundTiles.Length > 0))
            {
                if (RuntimeMapRenderer.Instance != null)
                {
                    RuntimeMapRenderer.Instance.RenderMapLayers(model);
                    int layerCount = model.groundLayers != null ? model.groundLayers.Count : 1;
                    Debug.Log($"[BaseBuildingManager] RenderMapLayers OK — {layerCount} lớp, {model.gridWidth}x{model.gridHeight}");
                }
                else
                {
                    // Nếu gặp lỗi này: RuntimeMapRenderer chưa được khởi tạo khi ImportLayoutFromModel được gọi.
                    // Kiểm tra LocalBaseBootstrap.Start() có await Task.Yield() sau SetupCamera() chưa.
                    Debug.LogError("[BaseBuildingManager] RuntimeMapRenderer.Instance == null! Map nền sẽ TRẮNG. " +
                                   "Kiểm tra SetupCamera() có chạy trước LoadBaseDataAsync() hay chưa.");
                }
            }

            ClearAllBuildings();

            foreach (var b in model.buildings)
            {
                await PlaceBuilding(b.id, b.x, b.y, b.level, b.state, b.flipX, b.instance_id);
            }
        }

        /// <summary>
        /// Chỉ import vị trí buildings từ model, KHÔNG render lại ground/terrain tiles.
        /// Dùng khi load data từ server (server map JSON không chứa groundLayers),
        /// để tránh xóa mất tiles nền đã được render từ DefaultMap.
        ///
        /// FIX CHO LỖI: Trắng map sau khi nâng cấp nhà + tắt PlayMode + đăng nhập lại.
        /// Nguyên nhân: ImportLayoutFromModel() gọi RenderMapLayers() nếu có groundLayers,
        /// nhưng server JSON không có → vẫn clear buildings → map nền bị mất vì ClearOldLayers()
        /// trong RenderMapLayers() không chạy nhưng buildings bị reset nên map rỗng.
        /// </summary>
        public async Task ImportBuildingsOnlyFromModel(BaseExportModel model)
        {
            if (model == null) return;

            if (model.gridWidth > BaseGridManager.Instance.Width || model.gridHeight > BaseGridManager.Instance.Height)
            {
                BaseGridManager.Instance.ExpandGrid(model.gridWidth, model.gridHeight);
            }

            // ✅ KHÔNG gọi RenderMapLayers() → ground tiles được giữ nguyên
            // ✅ KHÔNG gọi ClearAllBuildings() mà dùng ResetManager để tránh lỗi grid

            // Xóa buildings cũ nhưng KHÔNG đụng vào tilemaps/ground
            foreach (var b in _activeBuildings)
            {
                if (b != null)
                {
                    BaseGridManager.Instance.SetOccupied(b.GridX, b.GridY, b.Data.SizeX, b.Data.SizeY, false);
                    Destroy(b.gameObject);
                }
            }
            _activeBuildings.Clear();

            // Đặt lại buildings từ model
            foreach (var b in model.buildings)
            {
                await PlaceBuilding(b.id, b.x, b.y, b.level, b.state, b.flipX, b.instance_id);
            }
            
            Debug.Log($"[BaseBuildingManager] ImportBuildingsOnlyFromModel: đã đặt {model.buildings.Count} công trình (giữ nguyên nền map).");
        }

        public async Task SaveLayoutToServer()
        {
            if (!GameContext.HasCharacter)
            {
                Debug.LogWarning("[BaseBuildingManager] Chưa tạo nhân vật, bỏ qua lưu map.");
                return;
            }
            try
            {
                BaseExportModel model = new BaseExportModel
                {
                    gridWidth = BaseGridManager.Instance.Width,
                    gridHeight = BaseGridManager.Instance.Height
                };

                foreach (var b in _activeBuildings)
                {
                    if (b == null || b.Data == null) continue;
                    model.buildings.Add(new ExportedBuilding
                    {
                        instance_id = b.InstanceID,
                        id = b.Data.BuildingID,
                        x = b.GridX,
                        y = b.GridY,
                        level = b.CurrentLevel,
                        state = b.CurrentState,
                        flipX = b.FlipX
                    });
                }

                string json = JsonUtility.ToJson(model);
                var req = new GameClient.Network.Pb.SavePlayerMapRequest { MapJsonData = json };
                var res = await NetworkManager.Instance.GatewayClient.SavePlayerMapAsync(req, NetworkManager.DefaultCallOptions());
                if (res != null && res.Base != null && res.Base.Code == 0)
                {
                    Debug.Log("[BaseBuildingManager] Đã lưu map layout lên Server thành công!");
                }
                else
                {
                    Debug.LogWarning($"[BaseBuildingManager] Không thể lưu map lên Server: {res?.Base?.Message}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[BaseBuildingManager] Lỗi kết nối khi lưu map lên Server: {ex.Message}");
            }
        }
        public bool IsBuildingPlaced(long instanceId)
        {
            if (instanceId == 0) return false;
            return _activeBuildings.Exists(ab => ab.InstanceID == instanceId);
        }

        public void SyncBuildingsWithServerData(IEnumerable<GameClient.Network.Pb.Building> serverBuildings)
        {
            if (serverBuildings == null) return;
            var dict = new Dictionary<long, GameClient.Network.Pb.Building>();
            foreach (var sb in serverBuildings)
            {
                dict[sb.InstanceId] = sb;
            }

            foreach (var active in _activeBuildings)
            {
                if (active != null && active.Data != null && dict.TryGetValue(active.InstanceID, out var sb))
                {
                    // Calculate state based on UpgradeEndAt
                    BuildingState state = BuildingState.Normal;
                    if (sb.UpgradeEndAt > 0)
                    {
                        var endOffset = System.DateTimeOffset.FromUnixTimeSeconds(sb.UpgradeEndAt);
                        if (System.DateTimeOffset.UtcNow < endOffset)
                        {
                            state = BuildingState.Upgrading;
                        }
                    }
                    else
                    {
                        if (active.Data is ProductionBuildingData)
                        {
                            state = active.HasResourcesToHarvest() ? BuildingState.ReadyToHarvest : BuildingState.Producing;
                        }
                    }
                    
                    active.SyncUpgradeState(sb.Level, sb.UpgradeEndAt, state);
                }
            }
        }
        
        #endregion
    }
}
