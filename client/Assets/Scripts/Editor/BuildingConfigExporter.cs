# if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GameClient.Gameplay.BaseBuilder;

namespace GameClient.EditorTools
{
    public class BuildingConfigExporter : EditorWindow
    {
        [System.Serializable]
        public class ExportedBuildingRequirement
        {
            public string itemCode;
            public int quantity;
        }

        [System.Serializable]
        public class ExportedBuildingLevel
        {
            public int level;
            public int requiredReputation;
            public int buildTimeSeconds;
            public List<ExportedBuildingRequirement> costItems = new List<ExportedBuildingRequirement>();
        }

        [System.Serializable]
        public class ExportedBuildingData
        {
            public string buildingID;
            public string buildingNameKey;
            public string buildingDescKey;
            public string type;
            public int sizeX;
            public int sizeY;
            public string prefabAddress;
            public List<ExportedBuildingLevel> levelStats = new List<ExportedBuildingLevel>();
        }

        [System.Serializable]
        public class ExportedBuildingConfigList
        {
            public List<ExportedBuildingData> buildings = new List<ExportedBuildingData>();
        }

        [MenuItem("Tools/BaseBuilder/Export Building Configs")]
        public static void ShowWindow()
        {
            GetWindow<BuildingConfigExporter>("Building Config Exporter");
        }

        private void OnGUI()
        {
            GUILayout.Label("Base Builder Config Exporter", EditorStyles.boldLabel);
            GUILayout.Space(10);

            if (GUILayout.Button("Export Building Configs to JSON", GUILayout.Height(40)))
            {
                ExportConfigs();
            }
        }

        public static void ExportConfigs()
        {
            string[] guids = AssetDatabase.FindAssets("t:BuildingData");
            if (guids.Length == 0)
            {
                Debug.LogWarning("[BuildingConfigExporter] Không tìm thấy bất kỳ BuildingData ScriptableObject nào!");
                EditorUtility.DisplayDialog("Cảnh báo", "Không tìm thấy bất kỳ BuildingData ScriptableObject nào trong dự án!", "OK");
                return;
            }

            var exportList = new ExportedBuildingConfigList();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                BuildingData asset = AssetDatabase.LoadAssetAtPath<BuildingData>(path);

                if (asset == null) continue;

                var data = new ExportedBuildingData
                {
                    buildingID = asset.BuildingID,
                    buildingNameKey = asset.BuildingNameKey,
                    buildingDescKey = asset.BuildingDescKey,
                    type = asset.Type.ToString(),
                    sizeX = asset.SizeX,
                    sizeY = asset.SizeY,
                    prefabAddress = asset.PrefabAddress
                };

                if (asset.LevelStats != null)
                {
                    foreach (var lvl in asset.LevelStats)
                    {
                        var lvlData = new ExportedBuildingLevel
                        {
                            level = lvl.Level,
                            requiredReputation = lvl.RequiredReputation,
                            buildTimeSeconds = lvl.BuildTimeSeconds
                        };

                        if (lvl.CostItems != null)
                        {
                            foreach (var cost in lvl.CostItems)
                            {
                                lvlData.costItems.Add(new ExportedBuildingRequirement
                                {
                                    itemCode = cost.ItemCode,
                                    quantity = cost.Quantity
                                });
                            }
                        }

                        data.levelStats.Add(lvlData);
                    }
                }

                exportList.buildings.Add(data);
            }

            string json = JsonUtility.ToJson(exportList, true);

            // 1. Lưu vào Resources của Client
            string clientDir = Path.Combine(Application.dataPath, "Resources", "GameData");
            if (!Directory.Exists(clientDir))
            {
                Directory.CreateDirectory(clientDir);
            }
            string clientPath = Path.Combine(clientDir, "BuildingConfigs.json");
            File.WriteAllText(clientPath, json);
            Debug.Log($"[BuildingConfigExporter] Đã lưu cấu hình client tại: {clientPath}");

            // 2. Tự động đồng bộ sang Server
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..")); // Thư mục /client
            string serverDir = Path.GetFullPath(Path.Combine(projectRoot, "..", "server"));
            string serverConfigsDir = Path.Combine(serverDir, "configs");
            
            if (Directory.Exists(serverDir))
            {
                try
                {
                    if (!Directory.Exists(serverConfigsDir))
                    {
                        Directory.CreateDirectory(serverConfigsDir);
                    }
                    string serverPath = Path.Combine(serverConfigsDir, "building_configs.json");
                    File.WriteAllText(serverPath, json);
                    Debug.Log($"[BuildingConfigExporter] Tự động đồng bộ thành công sang Server tại: {serverPath}");

                    // Tự động đồng bộ dữ liệu vào Database qua Admin GM API
                    SyncToDatabase(exportList);

                    EditorUtility.DisplayDialog("Thành công", $"Đã xuất cấu hình sang Client, Server và đồng bộ Database thành công!\nTổng cộng: {exportList.buildings.Count} công trình.", "OK");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[BuildingConfigExporter] Lỗi khi tạo thư mục/ghi file phía Server: {ex.Message}");
                    EditorUtility.DisplayDialog("Thành công (Chỉ Client)", $"Đã xuất cấu hình Client thành công!\nLỗi đồng bộ Server: {ex.Message}", "OK");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Thành công (Chỉ Client)", $"Đã xuất cấu hình Client thành công!\nKhông tìm thấy thư mục Server tại {serverDir} để đồng bộ.", "OK");
            }

            AssetDatabase.Refresh();
        }

        [System.Serializable]
        public class ClientSyncBuildingReq
        {
            public string code;
            public string name;
            public int max_level;
            public string desc;
        }

        [System.Serializable]
        public class ClientSyncWrapper
        {
            public List<ClientSyncBuildingReq> list = new List<ClientSyncBuildingReq>();
        }

        private static void SyncToDatabase(ExportedBuildingConfigList configList)
        {
            var reqList = new List<ClientSyncBuildingReq>();
            foreach (var b in configList.buildings)
            {
                int maxLvl = 1;
                if (b.levelStats != null && b.levelStats.Count > 0)
                {
                    foreach (var lvl in b.levelStats)
                    {
                        if (lvl.level > maxLvl) maxLvl = lvl.level;
                    }
                }

                reqList.Add(new ClientSyncBuildingReq
                {
                    code = b.buildingID,
                    name = b.buildingNameKey, // Sử dụng làm tên thô, localise sau
                    max_level = maxLvl,
                    desc = b.buildingDescKey
                });
            }

            // Unity's JsonUtility does not support raw list root serialization. We serialize an array.
            // But Go server expects a JSON array at the root. We can use a trick or manually format a simple JSON array.
            var sb = new System.Text.StringBuilder();
            sb.Append("[");
            for (int i = 0; i < reqList.Count; i++)
            {
                sb.Append(JsonUtility.ToJson(reqList[i]));
                if (i < reqList.Count - 1) sb.Append(",");
            }
            sb.Append("]");

            try
            {
                using (var client = new System.Net.WebClient())
                {
                    client.Headers[System.Net.HttpRequestHeader.ContentType] = "application/json";
                    client.Encoding = System.Text.Encoding.UTF8;
                    string res = client.UploadString("http://localhost:8080/api/gm/buildings/sync", "POST", sb.ToString());
                    Debug.Log($"[BuildingConfigExporter] Đồng bộ Database qua GM API thành công: {res}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BuildingConfigExporter] Không thể đồng bộ Database tự động qua GM API (Server đang tắt?): {ex.Message}");
            }
        }
    }
}
# endif
