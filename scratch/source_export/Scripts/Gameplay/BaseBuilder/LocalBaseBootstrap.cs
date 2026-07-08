using UnityEngine;
using System.Threading.Tasks;
using System.Linq;
using GameClient.Managers;
using GameClient.Network;
using GameClient.UI;

namespace GameClient.Gameplay.BaseBuilder
{
    public class LocalBaseBootstrap : MonoBehaviour
    {
        private async void Start()
        {
            Debug.Log("[LocalBase] Đang khởi tạo Main Game...");

            SetupCamera();

            // ⏳ Chờ 1 frame để RuntimeMapRenderer.Awake() chạy xong
            await Task.Yield();

            // ⚡ Khởi tạo quá trình check thông tin người chơi song song để hiển thị UI tạo nhân vật ngay lập tức nếu cần
            _ = CheckPlayerAndOpenCreateCharPanelAsync();

            Debug.Log("[LocalBase] Đang tải Map...");
            await LoadBaseDataAsync();

            var cam = GameClient.BaseBuilding.Core.CameraController.Instance;
            if (cam != null)
            {
                var mainHall = BaseBuildingManager.Instance.GetFirstBuilding("main_hall");
                if (mainHall != null)
                {
                    cam.transform.position = new Vector3(mainHall.transform.position.x, mainHall.transform.position.y, cam.transform.position.z);
                }
            }

            if (GameClient.UI.SceneTransitionManager.Instance != null)
            {
                await GameClient.UI.SceneTransitionManager.Instance.ExitTransitionAsync();
            }
        }

        private async Task CheckPlayerAndOpenCreateCharPanelAsync()
        {
            try
            {
                var profileRes = await NetworkManager.Instance.GatewayClient.GetPlayerProfileAsync(new GameClient.Network.Pb.GetPlayerProfileRequest(), NetworkManager.DefaultCallOptions());
                if (profileRes != null && profileRes.Base != null && profileRes.Base.Code == 0 && profileRes.Profile != null)
                {
                    GameManager.Instance.SetPlayer(profileRes.Profile);
                    GameContext.HasCharacter = true;
                }

                // Tải danh sách các ải đã vượt từ server
                var stagesRes = await GameClient.Network.Api.SectBuildingApi.GetCompletedStagesAsync();
                if (stagesRes != null && stagesRes.Base != null && stagesRes.Base.Code == 0)
                {
                    GameManager.Instance.SetCompletedStages(stagesRes.StageIds);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[LocalBase] Không thể lấy thông tin nhân vật hoặc tiến trình ải: {ex.Message}");
            }

            var player = GameManager.Instance.CurrentPlayer;

            if (player == null || string.IsNullOrEmpty(player.Nickname))
            {
                Debug.Log("[LocalBase] Người chơi chưa có tên. Mở bảng tạo nhân vật CreateCharPanel...");
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.OpenPanel("CreateCharPanel");
                }
            }
            else
            {
                OpenMainHUD();
            }
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.bKey.wasPressedThisFrame)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.OpenPanel("UI_BuildMenuPanel", null, false);
                }
            }
        }

        private async Task<string> ReadStreamingAssetAsync(string fileName)
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
            if (path.Contains("://") || path.StartsWith("jar:file") || Application.platform == RuntimePlatform.Android)
            {
                using (var webRequest = UnityEngine.Networking.UnityWebRequest.Get(path))
                {
                    var operation = webRequest.SendWebRequest();
                    while (!operation.isDone)
                    {
                        await Task.Yield();
                    }
                    if (webRequest.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        return webRequest.downloadHandler.text;
                    }
                    else
                    {
                        Debug.LogError($"[LocalBase] Lỗi đọc {fileName} qua UnityWebRequest: {webRequest.error}");
                        return null;
                    }
                }
            }
            else
            {
                if (System.IO.File.Exists(path))
                {
                    try
                    {
                        return System.IO.File.ReadAllText(path);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[LocalBase] Lỗi đọc file {fileName}: {ex.Message}");
                        return null;
                    }
                }
            }
            return null;
        }

        private async Task LoadBaseDataAsync()
        {

            // Reset grid và manager để tránh data rò rỉ từ lần chạy trước
            if (BaseGridManager.Instance != null)
            {
                BaseGridManager.Instance.InitializeGrid(32, 32);
            }
            if (BaseBuildingManager.Instance != null)
            {
                BaseBuildingManager.Instance.ResetManager();
            }

            // ═══════════════════════════════════════════════════════════════
            // BƯỚC 1: Luôn load DefaultMap (địa hình + ground tiles) TRƯỚC
            // DefaultMap chứa groundLayers → RuntimeMapRenderer.RenderMapLayers() sẽ vẽ nền map
            // ═══════════════════════════════════════════════════════════════
            BaseExportModel defaultModel = null;

            // Ưu tiên load từ Resources (hoạt động trên mọi nền tảng kể cả Android APK)
            TextAsset defaultMapAsset = Resources.Load<TextAsset>("DefaultMap");
            Debug.Log($"[LocalBase][DEBUG] DefaultMapAsset từ Resources: {(defaultMapAsset != null ? "TÌM THẤY" : "KHÔNG TÌM THẤY")}");

            if (defaultMapAsset != null)
            {
                try
                {
                    Debug.Log($"[LocalBase][DEBUG] DefaultMap JSON length: {defaultMapAsset.text?.Length ?? 0} chars");
                    defaultModel = JsonUtility.FromJson<BaseExportModel>(defaultMapAsset.text);
                    if (defaultModel != null)
                    {
                        int groundLayerCount = defaultModel.groundLayers?.Count ?? 0;
                        int groundTileCount = defaultModel.groundTiles?.Length ?? 0;
                        Debug.Log($"[LocalBase][DEBUG] DefaultMap parsed: gridW={defaultModel.gridWidth} gridH={defaultModel.gridHeight} groundLayers={groundLayerCount} groundTiles={groundTileCount} buildings={defaultModel.buildings?.Count ?? 0}");

                        if (groundLayerCount == 0 && groundTileCount == 0)
                        {
                            Debug.LogError("[LocalBase][DEBUG] DefaultMap KHÔNG CÓ groundLayers hay groundTiles! File DefaultMap.json bị thiếu dữ liệu tiles nền.");
                        }

                        await BaseBuildingManager.Instance.ImportLayoutFromModel(defaultModel);
                        Debug.Log("[LocalBase] Đã nạp địa hình mặc định từ Resources/DefaultMap");
                    }
                    else
                    {
                        Debug.LogError("[LocalBase][DEBUG] JsonUtility.FromJson<BaseExportModel> trả về NULL từ DefaultMap!");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[LocalBase] Lỗi nạp địa hình từ Resources: {ex.Message}");
                }
            }

            // Fallback: đọc từ StreamingAssets nếu Resources không có
            if (defaultModel == null)
            {
                Debug.Log("[LocalBase][DEBUG] Thử load từ StreamingAssets/DefaultMap.json...");
                try
                {
                    string json = await ReadStreamingAssetAsync("DefaultMap.json");
                    Debug.Log($"[LocalBase][DEBUG] StreamingAssets JSON length: {json?.Length ?? 0} chars");
                    if (!string.IsNullOrEmpty(json))
                    {
                        defaultModel = JsonUtility.FromJson<BaseExportModel>(json);
                        if (defaultModel != null)
                        {
                            int groundLayerCount = defaultModel.groundLayers?.Count ?? 0;
                            int groundTileCount = defaultModel.groundTiles?.Length ?? 0;
                            Debug.Log($"[LocalBase][DEBUG] StreamingAssets DefaultMap: groundLayers={groundLayerCount} groundTiles={groundTileCount} buildings={defaultModel.buildings?.Count ?? 0}");
                            await BaseBuildingManager.Instance.ImportLayoutFromModel(defaultModel);
                            Debug.Log("[LocalBase] Đã nạp địa hình mặc định từ StreamingAssets");
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[LocalBase] Lỗi nạp địa hình từ StreamingAssets: {ex.Message}");
                }
            }

            if (defaultModel == null)
            {
                Debug.LogError("[LocalBase][DEBUG] Không load được DefaultMap từ bất kỳ nguồn nào! Map sẽ trắng.");
            }

            // ═══════════════════════════════════════════════════════════════
            // BƯỚC 2: Load vị trí buildings từ server, KHÔNG render lại ground
            // ═══════════════════════════════════════════════════════════════
            try
            {
                var res = await NetworkManager.Instance.GatewayClient.GetPlayerMapAsync(
                    new GameClient.Network.Pb.GetPlayerMapRequest(),
                    NetworkManager.DefaultCallOptions());

                Debug.Log($"[LocalBase][DEBUG] GetPlayerMapAsync: res={res != null} code={res?.Base?.Code} jsonLength={res?.MapJsonData?.Length ?? 0}");

                if (res != null && res.Base.Code == 0 && !string.IsNullOrEmpty(res.MapJsonData))
                {
                    // In ra 300 ký tự đầu của JSON để xem cấu trúc
                    string preview = res.MapJsonData.Length > 300 ? res.MapJsonData.Substring(0, 300) + "..." : res.MapJsonData;
                    Debug.Log($"[LocalBase][DEBUG] Server MapJsonData preview:\n{preview}");

                    BaseExportModel serverModel = JsonUtility.FromJson<BaseExportModel>(res.MapJsonData);
                    if (serverModel != null)
                    {
                        int srvGroundLayers = serverModel.groundLayers?.Count ?? 0;
                        int srvBuildings = serverModel.buildings?.Count ?? 0;
                        Debug.Log($"[LocalBase][DEBUG] serverModel: groundLayers={srvGroundLayers} buildings={srvBuildings}");

                        if (srvBuildings > 0)
                        {
                            await BaseBuildingManager.Instance.ImportBuildingsOnlyFromModel(serverModel);
                            Debug.Log("[LocalBase] Đã nạp vị trí công trình từ Server (giữ nguyên nền map)");
                        }
                    }
                    else
                    {
                        Debug.LogError("[LocalBase][DEBUG] JsonUtility.FromJson<BaseExportModel> trả về NULL từ server data!");
                    }
                }
                else
                {
                    Debug.LogWarning($"[LocalBase][DEBUG] Server không trả về map data hợp lệ. Code={res?.Base?.Code} json='{res?.MapJsonData?.Substring(0, Mathf.Min(50, res?.MapJsonData?.Length ?? 0))}'");
                }

                // Đồng bộ cấp độ + trạng thái nâng cấp từ DB
                var baseResp = await GameClient.Network.Api.SectBuildingApi.GetBaseAsync();
                if (baseResp != null && baseResp.Base.Code == 0)
                {
                    GameManager.Instance.SetBuildings(baseResp.Buildings);
                    BaseBuildingManager.Instance.SyncBuildingsWithServerData(baseResp.Buildings);
                    Debug.Log("[LocalBase] Đã đồng bộ cấp độ & trạng thái nâng cấp từ DB");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[LocalBase] Không thể tải map từ server (dùng layout mặc định): {ex.Message}");
            }
        }


        private void SetupCamera()
        {
            var cam = GameClient.BaseBuilding.Core.CameraController.Instance;
            if (cam != null)
            {
                Debug.Log("[LocalBase] Đã tìm thấy CameraController, cấu hình mặc định...");
            }

            if (Camera.main != null)
            {
                var raycaster = Camera.main.GetComponent<UnityEngine.EventSystems.Physics2DRaycaster>();
                if (raycaster == null)
                {
                    Camera.main.gameObject.AddComponent<UnityEngine.EventSystems.Physics2DRaycaster>();
                    Debug.Log("[LocalBase] Tự động thêm Physics2DRaycaster vào Main Camera.");
                }
            }

            var controller = FindFirstObjectByType<GameClient.BaseBuilding.Core.BuildingController>();
            if (controller == null)
            {
                var go = new GameObject("BuildingController");
                go.AddComponent<GameClient.BaseBuilding.Core.BuildingController>();
                Debug.Log("[LocalBase] Tự động tạo BuildingController vì không tìm thấy trong scene.");
            }

            var renderer = FindFirstObjectByType<GameClient.Gameplay.BaseBuilder.RuntimeMapRenderer>();
            if (renderer == null)
            {
                Debug.Log("[LocalBase] Không tìm thấy RuntimeMapRenderer trong Scene, đang tự động khởi tạo...");
                var envObj = new GameObject("EnvironmentManager");
                envObj.AddComponent<GameClient.BaseBuilding.Environment.SectEnvironmentManager>();
                envObj.AddComponent<GameClient.BaseBuilding.Environment.CloudManager>();
                var newRenderer = envObj.AddComponent<GameClient.Gameplay.BaseBuilder.RuntimeMapRenderer>();
                newRenderer.mappingConfig = Resources.Load<GameClient.Gameplay.BaseBuilder.TileToIDMapping>("GameData/MainTileMapping");
            }
        }

        private void OpenMainHUD()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenPanel("MainGameHUDPanel");
            }
        }
    }
}
