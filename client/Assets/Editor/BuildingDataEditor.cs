using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using GameClient.Gameplay.BaseBuilder;
using GameClient.Managers;

namespace GameClient.EditorTools
{
    [CustomEditor(typeof(BuildingData))]
    public class BuildingDataEditor : UnityEditor.Editor
    {
        private List<string> _availableItemIds = new List<string>();
        private List<string> _displayNames = new List<string>();
        private bool _hasLoadedConfigs = false;

        private void OnEnable()
        {
            LoadServerItemConfigs();
        }

        [System.Serializable]
        public class ServerItemConfig
        {
            public string item_code;
            public string name_key;
        }

        [System.Serializable]
        public class ServerItemConfigsResponse
        {
            public List<ServerItemConfig> items;
        }

        private void LoadServerItemConfigs()
        {
            _availableItemIds.Clear();
            _displayNames.Clear();

            // Mặc định ban đầu
            _availableItemIds.Add("gold");
            _displayNames.Add("gold (Vàng)");
            _availableItemIds.Add("qi");
            _displayNames.Add("qi (Linh Khí)");
            _availableItemIds.Add("diamond");
            _displayNames.Add("diamond (Kim Cương)");

            // Tải data thật từ Local Server Admin Dashboard (GM Tool)
            try
            {
                using (var client = new System.Net.WebClient())
                {
                    client.Encoding = System.Text.Encoding.UTF8;
                    string rawJson = client.DownloadString("http://localhost:8080/api/gm/item_configs");
                    string wrappedJson = "{ \"items\": " + rawJson + " }";
                    
                    var response = JsonUtility.FromJson<ServerItemConfigsResponse>(wrappedJson);
                    if (response != null && response.items != null)
                    {
                        // Nếu lấy được data thật thành công, làm sạch list mặc định để tránh trùng lặp
                        _availableItemIds.Clear();
                        _displayNames.Clear();

                        foreach (var item in response.items)
                        {
                            _availableItemIds.Add(item.item_code);
                            _displayNames.Add($"{item.item_code} ({item.name_key})");
                        }
                        Debug.Log($"[BuildingDataEditor] Nạp thành công {response.items.Count} vật phẩm THẬT từ Server.");
                    }
                }
            }
            catch (System.Exception serverEx)
            {
                Debug.LogWarning($"[BuildingDataEditor] Không kết nối được tới Local Server (localhost:8080): {serverEx.Message}. Tiến hành thử nạp từ Mock...");

                // Fallback nạp từ MockServerData.json nếu server offline
                try
                {
                    string path = Path.Combine(Application.streamingAssetsPath, "MockServerData.json");
                    if (File.Exists(path))
                    {
                        string json = File.ReadAllText(path);
                        ServerConfigData serverData = JsonUtility.FromJson<ServerConfigData>(json);
                        if (serverData != null && serverData.items != null)
                        {
                            foreach (var item in serverData.items)
                            {
                                if (!_availableItemIds.Contains(item.itemId))
                                {
                                    _availableItemIds.Add(item.itemId);
                                    _displayNames.Add($"{item.itemId} ({item.itemName})");
                                }
                            }
                        }
                    }
                }
                catch (System.Exception mockEx)
                {
                    Debug.LogWarning($"[BuildingDataEditor] Không nạp được MockServerData.json: {mockEx.Message}");
                }
            }

            _hasLoadedConfigs = _availableItemIds.Count > 0;
        }

        public override void OnInspectorGUI()
        {
            // Nút load lại cấu hình
            if (GUILayout.Button("Reload Item List from Mock Config", EditorStyles.miniButton))
            {
                LoadServerItemConfigs();
            }

            serializedObject.Update();

            // Vẽ các thuộc tính Identity mặc định
            SerializedProperty buildingID = serializedObject.FindProperty("BuildingID");
            SerializedProperty buildingNameKey = serializedObject.FindProperty("BuildingNameKey");
            SerializedProperty buildingDescKey = serializedObject.FindProperty("BuildingDescKey");
            SerializedProperty type = serializedObject.FindProperty("Type");
            SerializedProperty category = serializedObject.FindProperty("Category");

            EditorGUILayout.PropertyField(buildingID);
            EditorGUILayout.PropertyField(buildingNameKey);
            EditorGUILayout.PropertyField(buildingDescKey);
            EditorGUILayout.PropertyField(type);
            EditorGUILayout.PropertyField(category);

            // Vẽ Size
            SerializedProperty sizeX = serializedObject.FindProperty("SizeX");
            SerializedProperty sizeY = serializedObject.FindProperty("SizeY");
            EditorGUILayout.PropertyField(sizeX);
            EditorGUILayout.PropertyField(sizeY);

            // Vẽ Visual
            SerializedProperty prefabAddress = serializedObject.FindProperty("PrefabAddress");
            SerializedProperty visualConfig = serializedObject.FindProperty("VisualConfig");
            EditorGUILayout.PropertyField(prefabAddress);
            EditorGUILayout.PropertyField(visualConfig);

            // Vẽ Detail UI
            SerializedProperty detailSubPanelAddress = serializedObject.FindProperty("DetailSubPanelAddress");
            EditorGUILayout.PropertyField(detailSubPanelAddress);

            // Vẽ Reputation Limits
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Reputation Limits (Giới hạn Uy danh)", EditorStyles.boldLabel);
            SerializedProperty reputationLimits = serializedObject.FindProperty("ReputationLimits");
            EditorGUILayout.PropertyField(reputationLimits, true);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Level Stats & Requirements", EditorStyles.boldLabel);

            // Vẽ danh sách LevelStats
            SerializedProperty levelStats = serializedObject.FindProperty("LevelStats");
            for (int i = 0; i < levelStats.arraySize; i++)
            {
                SerializedProperty stat = levelStats.GetArrayElementAtIndex(i);
                SerializedProperty level = stat.FindPropertyRelative("Level");
                SerializedProperty reputation = stat.FindPropertyRelative("RequiredReputation");
                SerializedProperty buildTime = stat.FindPropertyRelative("BuildTimeSeconds");
                SerializedProperty costItems = stat.FindPropertyRelative("CostItems");

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"Level {level.intValue} Configuration", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(level);
                EditorGUILayout.PropertyField(reputation);
                EditorGUILayout.PropertyField(buildTime);

                // Vẽ danh sách CostItems yêu cầu nguyên liệu nâng cấp
                EditorGUILayout.LabelField("Costs (Nguyên Liệu Cần Thiết):");
                EditorGUILayout.BeginVertical("helpbox");
                
                int costCount = costItems.arraySize;
                for (int j = 0; j < costCount; j++)
                {
                    SerializedProperty req = costItems.GetArrayElementAtIndex(j);
                    SerializedProperty itemCodeProp = req.FindPropertyRelative("ItemCode");
                    SerializedProperty qtyProp = req.FindPropertyRelative("Quantity");

                    EditorGUILayout.BeginHorizontal();

                    // RENDER DROP LIST ITEM CHỌN NHANH
                    string currentCode = itemCodeProp.stringValue;
                    int selectedIdx = _availableItemIds.IndexOf(currentCode);
                    if (selectedIdx < 0) selectedIdx = 0;

                    if (_hasLoadedConfigs)
                    {
                        int newIdx = EditorGUILayout.Popup(selectedIdx, _displayNames.ToArray(), GUILayout.Width(200));
                        itemCodeProp.stringValue = _availableItemIds[newIdx];
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(itemCodeProp, GUIContent.none, GUILayout.Width(200));
                    }

                    // Nhập số lượng
                    EditorGUILayout.LabelField("Qty:", GUILayout.Width(30));
                    qtyProp.intValue = EditorGUILayout.IntField(qtyProp.intValue, GUILayout.Width(60));

                    // Nút xóa dòng
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("-", GUILayout.Width(20)))
                    {
                        costItems.DeleteArrayElementAtIndex(j);
                        break;
                    }
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.EndHorizontal();
                }

                // Nút thêm dòng nguyên liệu yêu cầu
                if (GUILayout.Button("+ Add Resource Cost", EditorStyles.miniButton))
                {
                    costItems.arraySize++;
                    SerializedProperty newReq = costItems.GetArrayElementAtIndex(costItems.arraySize - 1);
                    newReq.FindPropertyRelative("ItemCode").stringValue = _availableItemIds.Count > 0 ? _availableItemIds[0] : "gold";
                    newReq.FindPropertyRelative("Quantity").intValue = 100;
                }

                EditorGUILayout.EndVertical();

                // Nút Xóa Level này
                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                if (GUILayout.Button($"Delete Level {level.intValue} Config"))
                {
                    levelStats.DeleteArrayElementAtIndex(i);
                    break;
                }
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            // Nút Thêm Cấp Độ Mới
            if (GUILayout.Button("+ Add New Level Config", GUILayout.Height(30)))
            {
                levelStats.arraySize++;
                SerializedProperty newStat = levelStats.GetArrayElementAtIndex(levelStats.arraySize - 1);
                newStat.FindPropertyRelative("Level").intValue = levelStats.arraySize;
                newStat.FindPropertyRelative("RequiredReputation").intValue = 0;
                newStat.FindPropertyRelative("BuildTimeSeconds").intValue = 60;
                newStat.FindPropertyRelative("CostItems").arraySize = 0;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
