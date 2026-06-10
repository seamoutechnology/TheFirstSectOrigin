using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace GameClient.Editor.GMDashboard
{
    public class GMItemConfigTab
    {
        private EditorWindow window;
        private string AdminUrl => GMDashboardConfig.GmApiUrl;
        
        private List<GMItemConfigData> configList = new List<GMItemConfigData>();
        private GMItemConfigData selectedConfig = null;
        private int selectedIndex = -1;

        private Vector2 leftScrollPos;
        private Vector2 rightScrollPos;

        private List<GMItemEffect> currentEffects = new List<GMItemEffect>();
        private List<GMEffectConfigData> availableEffects = new List<GMEffectConfigData>();
        private string[] effectOptions = new string[0];

        public GMItemConfigTab(EditorWindow window)
        {
            this.window = window;
        }

        public void OnEnable()
        {
            FetchAllConfigs();
            FetchAllEffects();
        }

        private void FetchAllEffects()
        {
            string url = $"{AdminUrl}/effect_configs";
            var req = UnityWebRequest.Get(url);
            var op = req.SendWebRequest();
            
            op.completed += (asyncOp) =>
            {
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var array = GMJsonHelper.FromJson<GMEffectConfigData>(req.downloadHandler.text);
                    availableEffects = new List<GMEffectConfigData>(array ?? new GMEffectConfigData[0]);
                    effectOptions = new string[availableEffects.Count];
                    for (int i = 0; i < availableEffects.Count; i++)
                    {
                        effectOptions[i] = availableEffects[i].effect_code + " (" + availableEffects[i].name_key + ")";
                    }
                    window.Repaint();
                }
            };
        }

        public void OnGUI()
        {
            GUILayout.BeginHorizontal();

            // pANEL
            GUILayout.BeginVertical("box", GUILayout.Width(250));
            GUILayout.Label("Item Database", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh List")) FetchAllConfigs();
            if (GUILayout.Button("+ Create New Item"))
            {
                selectedConfig = new GMItemConfigData()
                {
                    item_code = "new_item_" + Random.Range(1000, 9999),
                    name_key = "New Item",
                    type = "CONSUMABLE",
                    rarity = "common",
                    icon = "",
                    desc_key = "Description",
                    max_stack = -1, // Thiết lập mặc định là không giới hạn
                    sources = "[]",
                    effects = "[]"
                };
                ParseEffects(selectedConfig.effects);
                selectedIndex = -1;
            }

            EditorGUILayout.Space();
            leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);
            for (int i = 0; i < configList.Count; i++)
            {
                var conf = configList[i];
                GUI.backgroundColor = (selectedIndex == i) ? Color.cyan : Color.white;
                if (GUILayout.Button(conf.item_code + " (" + conf.name_key + ")", EditorStyles.toolbarButton))
                {
                    selectedIndex = i;
                    selectedConfig = CloneConfig(conf);
                    ParseEffects(selectedConfig.effects);
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();

            // pANEL
            GUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
            if (selectedConfig != null)
            {
                GUILayout.Label("Edit Item: " + selectedConfig.item_code, EditorStyles.boldLabel);
                rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);

                bool isOnline = GMDashboardConfig.Status == GMDashboardConfig.ConnectionStatus.Online;

                EditorGUILayout.Space();
                selectedConfig.item_code = EditorGUILayout.TextField("Item Code (ID):", selectedConfig.item_code);
                selectedConfig.name_key = EditorGUILayout.TextField("Name Key (i18n):", selectedConfig.name_key);
                string[] itemTypes = { "CONSUMABLE", "EQUIPMENT", "MATERIAL", "SKIN_UNLOCKER", "FUNCTION_UNLOCKER", "VIP_LICENSE", "CURRENCY" };
                int typeIdx = System.Array.IndexOf(itemTypes, selectedConfig.type);
                if (typeIdx < 0) typeIdx = 0;
                typeIdx = EditorGUILayout.Popup("Type:", typeIdx, itemTypes);
                selectedConfig.type = itemTypes[typeIdx];

                string[] rarities = { "common", "uncommon", "rare", "epic", "legendary", "mythic" };
                int rarityIdx = System.Array.IndexOf(rarities, selectedConfig.rarity);
                if (rarityIdx < 0) rarityIdx = 0;
                rarityIdx = EditorGUILayout.Popup("Rarity:", rarityIdx, rarities);
                selectedConfig.rarity = rarities[rarityIdx];
                selectedConfig.icon = EditorGUILayout.TextField("Icon Path:", selectedConfig.icon);
                selectedConfig.max_stack = EditorGUILayout.IntField("Max Stack (-1 = Unlimited):", selectedConfig.max_stack);
                
                EditorGUILayout.LabelField("Description Key (i18n):");
                selectedConfig.desc_key = EditorGUILayout.TextArea(selectedConfig.desc_key, GUILayout.Height(40));

                EditorGUILayout.LabelField("Sources (JSON Array):");
                selectedConfig.sources = EditorGUILayout.TextArea(selectedConfig.sources, GUILayout.Height(40));

                EditorGUILayout.Space();
                GUILayout.Label("Item Effects", EditorStyles.boldLabel);
                for (int i = 0; i < currentEffects.Count; i++)
                {
                    GUILayout.BeginHorizontal("box");
                    GUILayout.BeginVertical();

                    int selectedEffectIdx = -1;
                    for (int j = 0; j < availableEffects.Count; j++)
                    {
                        if (availableEffects[j].effect_code == currentEffects[i].effect_code)
                        {
                            selectedEffectIdx = j;
                            break;
                        }
                    }
                    
                    int newIdx = EditorGUILayout.Popup("Effect:", selectedEffectIdx, effectOptions);
                    if (newIdx >= 0 && newIdx < availableEffects.Count)
                    {
                        currentEffects[i].effect_code = availableEffects[newIdx].effect_code;
                    }
                    
                    currentEffects[i].value = EditorGUILayout.FloatField("Value:", currentEffects[i].value);
                    currentEffects[i].min_value = EditorGUILayout.FloatField("Min Value:", currentEffects[i].min_value);
                    currentEffects[i].max_value = EditorGUILayout.FloatField("Max Value:", currentEffects[i].max_value);
                    
                    GUILayout.EndVertical();
                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("X", GUILayout.Width(30), GUILayout.ExpandHeight(true)))
                    {
                        currentEffects.RemoveAt(i);
                        i--;
                    }
                    GUI.backgroundColor = Color.white;
                    GUILayout.EndHorizontal();
                }
                if (GUILayout.Button("+ Add Effect"))
                {
                    string initEffect = availableEffects.Count > 0 ? availableEffects[0].effect_code : "none";
                    currentEffects.Add(new GMItemEffect { effect_code = initEffect, value = 10, min_value = 0, max_value = 100 });
                }

                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                GUI.enabled = isOnline;
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Save to Database", GUILayout.Height(30)))
                {
                    selectedConfig.effects = SerializeEffects();
                    SaveConfigToServer(selectedConfig);
                }
                
                // Nút Clone Item
                GUI.backgroundColor = new Color(0.2f, 0.6f, 1f); // Màu xanh dương nhạt cho nút Clone
                if (GUILayout.Button("Clone Item", GUILayout.Height(30)))
                {
                    selectedConfig.effects = SerializeEffects();
                    GMItemConfigData clone = CloneConfig(selectedConfig);
                    clone.item_code = selectedConfig.item_code + "_copy";
                    clone.name_key = selectedConfig.name_key + " (Copy)";
                    
                    // Chuyển đối tượng chỉnh sửa hiện tại sang bản copy vừa tạo
                    selectedConfig = clone;
                    ParseEffects(selectedConfig.effects);
                    selectedIndex = -1; // Reset index để đánh dấu đây là item mới chưa lưu
                    window.ShowNotification(new GUIContent("Đã nhân bản! Vui lòng đổi mã Item Code và bấm Save."));
                }
                
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Delete Item", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("Confirm Delete", "Are you sure you want to delete " + selectedConfig.item_code + "?", "Yes", "No"))
                    {
                        DeleteConfigFromServer(selectedConfig.item_code);
                    }
                }
                GUI.backgroundColor = Color.white;
                GUI.enabled = true;
                GUILayout.EndHorizontal();

                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("Select an item from the left or create a new one.", EditorStyles.centeredGreyMiniLabel);
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void ParseEffects(string json)
        {
            currentEffects.Clear();
            if (string.IsNullOrEmpty(json) || json == "[]") return;
            
            try
            {
                var array = GMJsonHelper.FromJson<GMItemEffect>(json);
                if (array != null) currentEffects.AddRange(array);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error parsing effects: " + ex.Message);
            }
        }

        [System.Serializable]
        private class EffectWrapper { public List<GMItemEffect> array; }

        private string SerializeEffects()
        {
            if (currentEffects.Count == 0) return "[]";
            string json = JsonUtility.ToJson(new EffectWrapper { array = currentEffects });
            json = json.Substring(9, json.Length - 10);
            return json;
        }

        private GMItemConfigData CloneConfig(GMItemConfigData source)
        {
            return new GMItemConfigData()
            {
                item_code = source.item_code,
                name_key = source.name_key,
                type = source.type,
                rarity = source.rarity,
                icon = source.icon,
                desc_key = source.desc_key,
                max_stack = source.max_stack,
                sources = source.sources,
                effects = source.effects
            };
        }

        private void FetchAllConfigs()
        {
            string url = $"{AdminUrl}/item_configs";
            var req = UnityWebRequest.Get(url);
            var op = req.SendWebRequest();
            
            op.completed += (asyncOp) =>
            {
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var array = GMJsonHelper.FromJson<GMItemConfigData>(req.downloadHandler.text);
                    configList = new List<GMItemConfigData>(array ?? new GMItemConfigData[0]);
                    window.Repaint();
                }
            };
        }

        private void SaveConfigToServer(GMItemConfigData config)
        {
            string url = $"{AdminUrl}/item_configs/save";
            string jsonBody = JsonUtility.ToJson(config);
            
            var req = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            var op = req.SendWebRequest();
            op.completed += (asyncOp) =>
            {
                if (req.result == UnityWebRequest.Result.Success)
                {
                    FetchAllConfigs();
                }
                else
                {
                    Debug.LogError($"[GM API] Save Item Config failed: {req.error}\n{req.downloadHandler.text}");
                }
            };
        }

        private void DeleteConfigFromServer(string itemCode)
        {
            string url = $"{AdminUrl}/item_configs/delete?code={itemCode}";
            var req = new UnityWebRequest(url, "DELETE");
            req.downloadHandler = new DownloadHandlerBuffer();
            
            var op = req.SendWebRequest();
            op.completed += (asyncOp) =>
            {
                if (req.result == UnityWebRequest.Result.Success)
                {
                    selectedConfig = null;
                    FetchAllConfigs();
                }
                else
                {
                    Debug.LogError($"[GM API] Delete Item Config failed: {req.error}\n{req.downloadHandler.text}");
                }
            };
        }
    }
}
