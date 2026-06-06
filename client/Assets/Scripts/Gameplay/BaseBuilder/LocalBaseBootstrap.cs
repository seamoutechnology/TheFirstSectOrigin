using UnityEngine;
using System.Threading.Tasks;
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

            Debug.Log("[LocalBase] Đang tải Map...");
            await LoadBaseDataAsync();

            if (GameClient.UI.SceneTransitionManager.Instance != null)
            {
                await GameClient.UI.SceneTransitionManager.Instance.ExitTransitionAsync();
            }

            try
            {
                var profileRes = await NetworkManager.Instance.GatewayClient.GetPlayerProfileAsync(new GameClient.Network.Pb.GetPlayerProfileRequest(), NetworkManager.DefaultCallOptions());
                if (profileRes != null && profileRes.Base != null && profileRes.Base.Code == 0 && profileRes.Profile != null)
                {
                    GameManager.Instance.SetPlayer(profileRes.Profile);
                    GameContext.HasCharacter = true;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[LocalBase] Không thể lấy thông tin nhân vật: {ex.Message}");
            }

            var player = GameManager.Instance.CurrentPlayer;
            
            if (player == null || string.IsNullOrEmpty(player.Nickname))
            {
                Debug.Log("[LocalBase] Người chơi chưa có tên. Chạy Cutscene tạo nhân vật...");
                
                string cutsceneId = "intro_cutscene";
                bool success = await CutsceneSyncManager.Instance.DownloadCutsceneAsync(cutsceneId);
                
                if (success)
                {
                    string jsonContent = CutsceneSyncManager.Instance.GetCutsceneJson(cutsceneId);
                    
                    var introObj = new GameObject("JsonCutscene_Runner");
                    var jsonCutscene = introObj.AddComponent<GameClient.Cutscenes.JsonCutscene>();
                    jsonCutscene.Initialize(jsonContent);
                    jsonCutscene.Play();
                }
                else
                {
                    Debug.LogError("[LocalBase] Không thể tải Cutscene!");
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
                    UIManager.Instance.OpenPanel("BuildMenuPanel");
                }
            }
        }

        private async Task LoadBaseDataAsync()
        {
            try
            {
                var res = await NetworkManager.Instance.GatewayClient.GetPlayerMapAsync(new GameClient.Network.Pb.GetPlayerMapRequest(), NetworkManager.DefaultCallOptions());
                if (res != null && res.Base.Code == 0 && !string.IsNullOrEmpty(res.MapJsonData))
                {
                    BaseExportModel model = JsonUtility.FromJson<BaseExportModel>(res.MapJsonData);
                    if (model != null)
                    {
                        await BaseBuildingManager.Instance.ImportLayoutFromModel(model);
                        Debug.Log($"[LocalBase] Đã nạp thành công map layout từ Server");
                        return;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[LocalBase] Không thể lấy map từ server: {ex.Message}");
            }

            string mapFilePath = System.IO.Path.Combine(Application.streamingAssetsPath, "DefaultMap.json");
            if (System.IO.File.Exists(mapFilePath))
            {
                string json = System.IO.File.ReadAllText(mapFilePath);
                BaseExportModel model = JsonUtility.FromJson<BaseExportModel>(json);
                if (model != null)
                {
                    await BaseBuildingManager.Instance.ImportLayoutFromModel(model);
                    Debug.Log($"[LocalBase] Đã nạp thành công map layout từ {mapFilePath}");
                }
            }
            else
            {
                Debug.LogWarning("[LocalBase] Không tìm thấy DefaultMap.json, dùng map trống.");
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
            }
        }
    }
}
