using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace GameClient.Editor.GMDashboard
{
    public class GMMissionConfigTab
    {
        private EditorWindow window;
        private string AdminUrl => GMDashboardConfig.GmApiUrl;

        private List<GMMissionTemplateData> configList = new List<GMMissionTemplateData>();
        private GMMissionTemplateData selectedConfig = null;
        private int selectedIndex = -1;

        private Vector2 leftScrollPos;
        private Vector2 rightScrollPos;

        public GMMissionConfigTab(EditorWindow window)
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

            // Left Panel: list of missions
            GUILayout.BeginVertical("box", GUILayout.Width(250));
            GUILayout.Label("Mission Templates", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh List")) FetchAllConfigs();
            if (GUILayout.Button("+ Create Mission"))
            {
                selectedConfig = new GMMissionTemplateData()
                {
                    mission_id = configList.Count > 0 ? configList[configList.Count - 1].mission_id + 1 : 1,
                    title = "New Mission",
                    description = "Mission Description",
                    type = 1, // MAIN
                    target_type = "build_upgrade",
                    target_param = "main_hall",
                    target_progress = 1,
                    rewards = "{}"
                };
                selectedIndex = -1;
            }

            EditorGUILayout.Space();
            leftScrollPos = EditorGUILayout.BeginScrollView(leftScrollPos);
            for (int i = 0; i < configList.Count; i++)
            {
                var conf = configList[i];
                GUI.backgroundColor = (selectedIndex == i) ? Color.cyan : Color.white;
                if (GUILayout.Button(conf.mission_id + ". " + conf.title, EditorStyles.toolbarButton))
                {
                    selectedIndex = i;
                    selectedConfig = CloneConfig(conf);
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();

            // Right Panel: details
            GUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
            if (selectedConfig != null)
            {
                GUILayout.Label("Edit Mission: #" + selectedConfig.mission_id, EditorStyles.boldLabel);
                rightScrollPos = EditorGUILayout.BeginScrollView(rightScrollPos);

                EditorGUILayout.Space();
                selectedConfig.mission_id = EditorGUILayout.IntField("Mission ID:", selectedConfig.mission_id);
                selectedConfig.title = EditorGUILayout.TextField("Title:", selectedConfig.title);
                
                EditorGUILayout.LabelField("Description:");
                selectedConfig.description = EditorGUILayout.TextArea(selectedConfig.description, GUILayout.Height(40));

                string[] missionTypes = { "DAILY", "MAIN", "SIDE", "SECT" };
                selectedConfig.type = EditorGUILayout.Popup("Mission Type:", selectedConfig.type, missionTypes);

                string[] targetTypes = { "player_level", "build_upgrade", "craft_item" };
                int targetIdx = System.Array.IndexOf(targetTypes, selectedConfig.target_type);
                if (targetIdx < 0) targetIdx = 0;
                targetIdx = EditorGUILayout.Popup("Target Action:", targetIdx, targetTypes);
                selectedConfig.target_type = targetTypes[targetIdx];

                selectedConfig.target_param = EditorGUILayout.TextField("Target Param (e.g. building code):", selectedConfig.target_param);
                selectedConfig.target_progress = EditorGUILayout.IntField("Target Progress Count:", selectedConfig.target_progress);

                EditorGUILayout.LabelField("Rewards (JSON e.g. {\"gold\":100}):");
                selectedConfig.rewards = EditorGUILayout.TextArea(selectedConfig.rewards, GUILayout.Height(40));

                EditorGUILayout.Space();
                GUILayout.BeginHorizontal();
                bool missionOnline = GMDashboardConfig.Status == GMDashboardConfig.ConnectionStatus.Online;
                GUI.enabled = missionOnline;
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Save to Database", GUILayout.Height(30)))
                {
                    SaveConfigToServer(selectedConfig);
                }
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Delete Mission", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("Confirm Delete", "Are you sure you want to delete mission " + selectedConfig.mission_id + "?", "Yes", "No"))
                    {
                        DeleteConfigFromServer(selectedConfig.mission_id);
                    }
                }
                GUI.backgroundColor = Color.white;
                GUI.enabled = true;
                GUILayout.EndHorizontal();

                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("Select a mission template from the left list to edit.", EditorStyles.centeredGreyMiniLabel);
            }
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private GMMissionTemplateData CloneConfig(GMMissionTemplateData source)
        {
            return new GMMissionTemplateData()
            {
                mission_id = source.mission_id,
                title = source.title,
                description = source.description,
                type = source.type,
                target_type = source.target_type,
                target_param = source.target_param,
                target_progress = source.target_progress,
                rewards = source.rewards
            };
        }

        private void FetchAllConfigs()
        {
            string url = $"{AdminUrl}/mission_templates";
            var req = UnityWebRequest.Get(url);
            var op = req.SendWebRequest();

            op.completed += (asyncOp) =>
            {
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var array = GMJsonHelper.FromJson<GMMissionTemplateData>(req.downloadHandler.text);
                    configList = new List<GMMissionTemplateData>(array ?? new GMMissionTemplateData[0]);
                    window.Repaint();
                }
            };
        }

        private void SaveConfigToServer(GMMissionTemplateData config)
        {
            string url = $"{AdminUrl}/mission_templates/save";
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
                    Debug.LogError($"[GM API] Save Mission Template failed: {req.error}\n{req.downloadHandler.text}");
                }
            };
        }

        private void DeleteConfigFromServer(int missionID)
        {
            string url = $"{AdminUrl}/mission_templates/delete?id={missionID}";
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
                    Debug.LogError($"[GM API] Delete Mission Template failed: {req.error}\n{req.downloadHandler.text}");
                }
            };
        }
    }
}
