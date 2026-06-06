using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using GameClient.Core;
using GameClient.Managers;

namespace GameClient.Gameplay.BaseBuilder
{
    public class BaseBuildingManager : Singleton<BaseBuildingManager>
    {
        private List<BuildingInstance> _activeBuildings = new List<BuildingInstance>();

        private Dictionary<string, BuildingData> _buildingDatabase = new Dictionary<string, BuildingData>();

        public Dictionary<string, BuildingData> GetBuildingDatabase() => _buildingDatabase;

        public BuildingInstance GetFirstBuilding(string buildingId)
        {
            return _activeBuildings.Find(b => b.Data != null && b.Data.BuildingID == buildingId);
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

        public async Task<bool> PlaceBuilding(string buildingId, int gridX, int gridY, int level = 1, BuildingState state = BuildingState.Normal, bool flipX = false)
        {
            if (!_buildingDatabase.TryGetValue(buildingId, out BuildingData data))
            {
                Debug.LogWarning($"[BaseBuilding] Không tìm thấy dữ liệu nhà ID: {buildingId}. Sẽ dùng Mock Data.");
                data = ScriptableObject.CreateInstance<BuildingData>();
                data.BuildingID = buildingId;
                
                if (buildingId == "main_hall")
                {
                    data.SizeX = 4;
                    data.SizeY = 4;
                }
                else
                {
                    data.SizeX = 2;
                    data.SizeY = 2;
                }
                
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
                
                var collider = go.AddComponent<BoxCollider2D>();
                collider.size = new Vector2(data.SizeX * BaseGridManager.TILE_WIDTH, data.SizeY * BaseGridManager.TILE_HEIGHT);
                
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
            instance.Setup(data, gridX, gridY, level, state, flipX);
            
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

            if (RuntimeMapRenderer.Instance != null)
            {
                RuntimeMapRenderer.Instance.RenderMapLayers(model);
            }

            ClearAllBuildings();

            foreach (var b in model.buildings)
            {
                await PlaceBuilding(b.id, b.x, b.y, b.level, b.state, b.flipX);
            }
        }
        
        #endregion
    }
}
