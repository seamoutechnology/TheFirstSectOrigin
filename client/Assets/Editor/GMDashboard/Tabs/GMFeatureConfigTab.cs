using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace GameClient.Editor.GMDashboard
{
    public class GMFeatureConfigTab
    {
        private EditorWindow window;
        private string AdminUrl => GMDashboardConfig.GmApiUrl;

        private List<GMFeatureConfigData> configList = new List<GMFeatureConfigData>();
        private GMFeatureConfigData selectedConfig = null;
        private int selectedIndex = -1;

        private Vector2 leftScrollPos;
        private Vector2 rightScrollPos;

        public GMFeatureConfigTab(EditorWindow window)
        {
            this.window = window;
        }

        public void OnEnable()
        {
            FetchAllConfigs();
        }

        public void OnGUI()
        {
            GUILayout.BeginHorizontal();

            // Left panel: List
            GUILayout.BeginVertical("box", GUILayout.Width(250));
            GUILayout.Label("Feature Lock Configs", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh List")) FetchAllConfigs();
            if (GUILayout.Button("+ Create Feature Lock"))
            {
                selectedConfig = new GMFeatureConfigData()
                {
                    feature_code = "new_feature_" + Random.Range(100, 999),
                    name_key = "New Feature",
                    icon = "ui/icons/default_icon",
                    required_player_level = 1,
                    required_mission_code = "",
                    is_active = true
                };
                selectedIndex = -1;
            }

            EditorGUILayout.Space();
            leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);
            for (int i = 0; i < configList.Count; i++)
            {
                var conf = configList[i];
                GUI.backgroundColor = (selectedIndex == i) ? Color.cyan : Color.white;
                if (GUILayout.Button(conf.feature_code + " (" + conf.name_key + ")", EditorStyles.toolbarButton))
                {
                    selectedIndex = i;
                    selectedConfig = CloneConfig(conf);
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();

            // Right panel: Details
            GUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
            if (selectedConfig != null)
            {
                GUILayout.Label("Edit Feature: " + selectedConfig.feature_code, EditorStyles.boldLabel);
                rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);

                EditorGUILayout.Space();
                selectedConfig.feature_code = EditorGUILayout.TextField("Feature Code (ID):", selectedConfig.feature_code);
                selectedConfig.name_key = EditorGUILayout.TextField("Name Key (i18n):", selectedConfig.name_key);
                selectedConfig.icon = EditorGUILayout.TextField("Icon Sprite Path:", selectedConfig.icon);
                selectedConfig.required_player_level = EditorGUILayout.IntField("Req Player Level:", selectedConfig.required_player_level);
                selectedConfig.required_mission_code = EditorGUILayout.TextField("Req Mission Code:", selectedConfig.required_mission_code);
                selectedConfig.is_active = EditorGUILayout.Toggle("Is Active?", selectedConfig.is_active);

                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                bool featureOnline = GMDashboardConfig.Status == GMDashboardConfig.ConnectionStatus.Online;
                GUI.enabled = featureOnline;
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Save to Database", GUILayout.Height(30)))
                {
                    SaveConfigToServer(selectedConfig);
                }
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Delete Config", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("Confirm Delete", "Are you sure you want to delete " + selectedConfig.feature_code + "?", "Yes", "No"))
                    {
                        DeleteConfigFromServer(selectedConfig.feature_code);
                    }
                }
                GUI.backgroundColor = Color.white;
                GUI.enabled = true;
                GUILayout.EndHorizontal();

                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("Select a feature lock configuration from the left panel.", EditorStyles.centeredGreyMiniLabel);
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private GMFeatureConfigData CloneConfig(GMFeatureConfigData source)
        {
            return new GMFeatureConfigData()
            {
                feature_code = source.feature_code,
                name_key = source.name_key,
                icon = source.icon,
                required_player_level = source.required_player_level,
                required_mission_code = source.required_mission_code,
                is_active = source.is_active
            };
        }

        private void FetchAllConfigs()
        {
            string url = $"{AdminUrl}/feature_configs";
            var req = UnityWebRequest.Get(url);
            var op = req.SendWebRequest();

            op.completed += (asyncOp) =>
            {
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var array = GMJsonHelper.FromJson<GMFeatureConfigData>(req.downloadHandler.text);
                    configList = new List<GMFeatureConfigData>(array ?? new GMFeatureConfigData[0]);
                    window.Repaint();
                }
            };
        }

        private void SaveConfigToServer(GMFeatureConfigData config)
        {
            string url = $"{AdminUrl}/feature_configs/save";
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
                    Debug.LogError($"[GM API] Save Feature Config failed: {req.error}\n{req.downloadHandler.text}");
                }
            };
        }

        private void DeleteConfigFromServer(string code)
        {
            string url = $"{AdminUrl}/feature_configs/delete?code={code}";
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
                    Debug.LogError($"[GM API] Delete Feature Config failed: {req.error}\n{req.downloadHandler.text}");
                }
            };
        }
    }
}
