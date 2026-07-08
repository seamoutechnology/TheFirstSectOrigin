using UnityEngine;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using GameClient.Core;

namespace GameClient.Managers
{
    public enum MapType
    {
        None = -1,
        LocalBase = 0,  // Map xây dựng của riêng người chơi
        Dungeon,    // Map phụ bản (PVE)
        WorldMap    // Map thế giới lớn (SLG/MMO)
    }

    public class MapManager : Singleton<MapManager>
    {
        public MapType CurrentMap { get; private set; } = MapType.None;
        
        private const string SCENE_LOCAL_BASE = "LocalBase";
        private const string SCENE_DUNGEON = "Dungeon";
        private const string SCENE_WORLD_MAP = "WorldMap";

        public async Task LoadMapAsync(MapType mapType)
        {
            if (CurrentMap == mapType && SceneManager.GetActiveScene().name == GetSceneName(mapType))
            {
                Debug.LogWarning($"[MapManager] Đang ở sẵn map {mapType} rồi!");
                return;
            }

            Debug.Log($"[MapManager] Chuẩn bị nhảy sang Map: {mapType}...");

            if (GameClient.UI.SceneTransitionManager.Instance != null)
            {
                await GameClient.UI.SceneTransitionManager.Instance.EnterTransitionAsync();
            }
            else
            {
                UIManager.Instance.ShowMessage("Đang Tải...", "Đang dịch chuyển không gian, vui lòng đợi...");
            }

            string sceneName = GetSceneName(mapType);
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ClearAllPanels(new string[] { "OverlayUI" });
            }
            
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false; // Tạm giữ lại không cho load xong ngay (Để làm hiệu ứng)

            while (op.progress < 0.9f)
            {
                await Task.Delay(100); 
            }

            op.allowSceneActivation = true;

            while (!op.isDone)
            {
                await Task.Delay(50);
            }

            CurrentMap = mapType;
            Debug.Log($"[MapManager] Đã load xong Map: {mapType}");
            
            if (GameClient.UI.SceneTransitionManager.Instance != null)
            {
                await GameClient.UI.SceneTransitionManager.Instance.ExitTransitionAsync();
            }
            
            if (AudioManager.Instance != null)
            {
                switch (mapType)
                {
                    case MapType.LocalBase:
                        AudioManager.Instance.PlayMusic(GameConstants.Audio.BGM_LOCAL_BASE, true);
                        break;
                    case MapType.Dungeon:
                        AudioManager.Instance.PlayMusic(GameConstants.Audio.BGM_DUNGEON, true);
                        break;
                    case MapType.WorldMap:
                        AudioManager.Instance.PlayMusic(GameConstants.Audio.BGM_WORLD, true);
                        break;
                    default:
                        AudioManager.Instance.StopMusic();
                        break;
                }
            }
            
            if (mapType == MapType.LocalBase)
            {
                if (GameObject.FindFirstObjectByType<GameClient.Gameplay.BaseBuilder.LocalBaseBootstrap>() == null)
                {
                    Debug.Log("[MapManager] Không tìm thấy LocalBaseBootstrap trong Scene, đang tự động khởi tạo...");
                    
                    var scopeObj = new GameObject("LocalBaseLifetimeScope");
                    scopeObj.AddComponent<GameClient.Gameplay.BaseBuilder.LocalBaseLifetimeScope>();

                    var bootstrapObj = new GameObject("LocalBaseBootstrap");
                    bootstrapObj.AddComponent<GameClient.Gameplay.BaseBuilder.LocalBaseBootstrap>();
                    
                    var gridObj = new GameObject("GridManager");
                    gridObj.AddComponent<GameClient.BaseBuilding.Grid.GridManager>();
                    
                    var buildCtrlObj = new GameObject("BuildingController");
                    var buildCtrl = buildCtrlObj.AddComponent<GameClient.BaseBuilding.Core.BuildingController>();
                    
                    var previewObj = new GameObject("BuildingPreview");
                    previewObj.transform.SetParent(buildCtrlObj.transform);
                    var previewSr = previewObj.AddComponent<SpriteRenderer>();
                    
                    Texture2D tex = new Texture2D(1, 1);
                    tex.SetPixel(0, 0, new Color(1, 1, 1, 0.5f));
                    tex.Apply();
                    previewSr.sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
                    previewSr.gameObject.SetActive(false);
                    buildCtrl.previewRenderer = previewSr;

                    var mainCam = Camera.main;
                    if (mainCam != null && mainCam.GetComponent<GameClient.BaseBuilding.Core.CameraController>() == null)
                    {
                        mainCam.gameObject.AddComponent<GameClient.BaseBuilding.Core.CameraController>();
                        if (mainCam != null)
                        {
                            mainCam.orthographic = true;
                            mainCam.orthographicSize = 5f;
                            mainCam.transform.position = new Vector3(0, 0, -10);
                            mainCam.transform.rotation = Quaternion.identity;
                            
                            mainCam.clearFlags = CameraClearFlags.SolidColor;
                            mainCam.backgroundColor = new Color(0.6f, 0.8f, 1f); 
                        }
                    }

                    var environmentObj = new GameObject("EnvironmentManager");
                    environmentObj.AddComponent<GameClient.BaseBuilding.Environment.SectEnvironmentManager>();
                    environmentObj.AddComponent<GameClient.BaseBuilding.Environment.CloudManager>();
                    var renderer = environmentObj.AddComponent<GameClient.Gameplay.BaseBuilder.RuntimeMapRenderer>();
                     renderer.mappingConfig = Resources.Load<GameClient.Gameplay.BaseBuilder.TileToIDMapping>("GameData/MainTileMapping");
                }
            }
            else if (mapType == MapType.Dungeon)
            {
                if (GameObject.FindFirstObjectByType<GameClient.Gameplay.Combat.CombatSceneController>() == null)
                {
                    Debug.Log("[MapManager] Không tìm thấy CombatSceneController trong Scene Dungeon, đang tự động khởi tạo...");
                    var combatCtrlObj = new GameObject("CombatController");
                    combatCtrlObj.AddComponent<GameClient.Gameplay.Combat.CombatSceneController>();

                    // Cấu hình Main Camera cho Combat
                    var mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        mainCam.orthographic = true;
                        mainCam.orthographicSize = 5f;
                        mainCam.transform.position = new Vector3(0, 0, -10);
                        mainCam.transform.rotation = Quaternion.identity;
                        mainCam.clearFlags = CameraClearFlags.SolidColor;
                        mainCam.backgroundColor = new Color(0.15f, 0.15f, 0.15f); // Background tối
                    }
                }
            }
        }

        private string GetSceneName(MapType type)
        {
            switch (type)
            {
                case MapType.LocalBase: return SCENE_LOCAL_BASE;
                case MapType.Dungeon: return SCENE_DUNGEON;
                case MapType.WorldMap: return SCENE_WORLD_MAP;
                default: return SCENE_LOCAL_BASE;
            }
        }
    }
}
