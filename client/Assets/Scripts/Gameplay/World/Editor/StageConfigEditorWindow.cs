using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using GameClient.Gameplay.World;

namespace GameClient.Editor
{
    public class StageConfigEditorWindow : EditorWindow
    {
        private const string API_GET_STAGES = "http://localhost:8080/api/gm/stages";
        private const string API_SYNC_STAGES = "http://localhost:8080/api/gm/stages/sync";
        private const string STAGES_FOLDER_PATH = "Assets/Resources/GameData/Stages";

        private List<StageDataRepresentation> _serverStages = new List<StageDataRepresentation>();
        private int _selectedStageIndex = -1;
        private Vector2 _scrollPos;

        [MenuItem("Tools/PVE Stage Manager (Cloud Sync)")]
        public static void ShowWindow()
        {
            var window = GetWindow<StageConfigEditorWindow>();
            window.titleContent = new GUIContent("PVE Stage Manager");
            window.minSize = new Vector2(600, 500);
            window.Show();
        }

        private void OnEnable()
        {
            PullFromServer();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            // Left panel - list of stages
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(220), GUILayout.ExpandHeight(true));
            GUILayout.Label("Stages List", EditorStyles.boldLabel);

            if (GUILayout.Button("Pull from Server", GUILayout.Height(30)))
            {
                PullFromServer();
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            for (int i = 0; i < _serverStages.Count; i++)
            {
                var stage = _serverStages[i];
                string displayName = string.IsNullOrEmpty(stage.stageName) ? stage.stageId : stage.stageName;
                if (GUILayout.Toggle(_selectedStageIndex == i, $"[{stage.stageId}] {displayName}", "Button", GUILayout.Height(25)))
                {
                    _selectedStageIndex = i;
                }
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Create New Stage", GUILayout.Height(30)))
            {
                CreateNewLocalStage();
            }
            EditorGUILayout.EndVertical();

            // Right panel - stage detail editor
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (_selectedStageIndex >= 0 && _selectedStageIndex < _serverStages.Count)
            {
                DrawStageDetail(_serverStages[_selectedStageIndex]);
            }
            else
            {
                GUILayout.Label("Select a stage from the list or create a new one to start editing.", EditorStyles.centeredGreyMiniLabel);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            // Footer - sync buttons
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            if (GUILayout.Button("Save Local ScriptableObjects", GUILayout.Height(35)))
            {
                SaveAllLocalAssets();
            }
            if (GUILayout.Button("Push & Sync to Go Server", GUILayout.Height(35)))
            {
                PushToServer();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStageDetail(StageDataRepresentation stage)
        {
            GUILayout.Label($"Editing Stage: {stage.stageId}", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            stage.stageId = EditorGUILayout.TextField("Stage ID", stage.stageId);
            stage.stageName = EditorGUILayout.TextField("Stage Name Key", stage.stageName);
            stage.description = EditorGUILayout.TextField("Description Key", stage.description);
            stage.recommendPower = EditorGUILayout.IntField("Recommend Power", stage.recommendPower);
            stage.staminaCost = EditorGUILayout.IntField("Stamina Cost", stage.staminaCost);
            stage.combatSceneName = EditorGUILayout.TextField("Combat Scene Name", stage.combatSceneName);

            EditorGUILayout.Space();
            GUILayout.Label("Enemies (Monsters)", EditorStyles.boldLabel);

            if (stage.enemiesConfig == null) stage.enemiesConfig = new List<MonsterConfigRepresentation>();

            for (int i = 0; i < stage.enemiesConfig.Count; i++)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Enemy #{i + 1}", EditorStyles.miniBoldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    stage.enemiesConfig.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                var enemy = stage.enemiesConfig[i];
                enemy.monsterId = EditorGUILayout.TextField("Monster Template ID", enemy.monsterId);
                enemy.name = EditorGUILayout.TextField("Monster Name Key", enemy.name);
                enemy.level = EditorGUILayout.IntField("Level", enemy.level);
                enemy.maxHP = EditorGUILayout.IntField("Max HP", enemy.maxHP);
                enemy.attack = EditorGUILayout.IntField("Attack", enemy.attack);
                enemy.defense = EditorGUILayout.IntField("Defense", enemy.defense);
                enemy.speed = EditorGUILayout.IntField("Speed", enemy.speed);
                enemy.isBoss = EditorGUILayout.Toggle("Is Boss", enemy.isBoss);
                enemy.prefabAddress = EditorGUILayout.TextField("Prefab Address (Visual)", enemy.prefabAddress);
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Enemy"))
            {
                stage.enemiesConfig.Add(new MonsterConfigRepresentation { name = "new_enemy", level = 1, maxHP = 100, attack = 10, defense = 5, speed = 10 });
            }
        }

        private void PullFromServer()
        {
            var request = UnityWebRequest.Get(API_GET_STAGES);
            var asyncOp = request.SendWebRequest();

            asyncOp.completed += (op) =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    var dbList = JsonUtility.FromJson<Wrapper<StageConfigDBRepresentation>>($"{{\"items\":{json}}}");
                    _serverStages.Clear();
                    if (dbList != null && dbList.items != null)
                    {
                        foreach (var dbItem in dbList.items)
                        {
                            var stage = JsonUtility.FromJson<StageDataRepresentation>(dbItem.json_data);
                            if (stage != null)
                            {
                                _serverStages.Add(stage);
                            }
                        }
                    }
                    Repaint();
                    Debug.Log("[StageConfigEditorWindow] Successfully pulled PVE stages from Go Server.");
                }
                else
                {
                    Debug.LogError($"[StageConfigEditorWindow] Failed to pull from server: {request.error}");
                }
                request.Dispose();
            };
        }

        private void CreateNewLocalStage()
        {
            string newId = $"stage_{_serverStages.Count + 1}";
            _serverStages.Add(new StageDataRepresentation
            {
                stageId = newId,
                stageName = $"{newId}_title",
                description = $"{newId}_desc",
                recommendPower = 1000,
                staminaCost = 5,
                combatSceneName = "Dungeon",
                enemiesConfig = new List<MonsterConfigRepresentation>()
            });
            _selectedStageIndex = _serverStages.Count - 1;
        }

        private void SaveAllLocalAssets()
        {
            if (!Directory.Exists(STAGES_FOLDER_PATH))
            {
                Directory.CreateDirectory(STAGES_FOLDER_PATH);
            }

            foreach (var stageRep in _serverStages)
            {
                string assetPath = $"{STAGES_FOLDER_PATH}/Stage_{stageRep.stageId}.asset";
                StageData asset = AssetDatabase.LoadAssetAtPath<StageData>(assetPath);
                bool isNew = asset == null;

                if (isNew)
                {
                    asset = CreateInstance<StageData>();
                }

                asset.stageId = stageRep.stageId;
                asset.stageName = stageRep.stageName;
                asset.description = stageRep.description;
                asset.recommendPower = stageRep.recommendPower;
                asset.staminaCost = stageRep.staminaCost;
                asset.combatSceneName = stageRep.combatSceneName;

                asset.enemiesConfig = new List<MonsterConfig>();
                foreach (var monsterRep in stageRep.enemiesConfig)
                {
                    asset.enemiesConfig.Add(new MonsterConfig
                    {
                        monsterId = monsterRep.monsterId,
                        name = monsterRep.name,
                        level = monsterRep.level,
                        maxHP = monsterRep.maxHP,
                        attack = monsterRep.attack,
                        defense = monsterRep.defense,
                        speed = monsterRep.speed,
                        isBoss = monsterRep.isBoss,
                        prefabAddress = monsterRep.prefabAddress
                    });
                }

                if (isNew)
                {
                    AssetDatabase.CreateAsset(asset, assetPath);
                }
                else
                {
                    EditorUtility.SetDirty(asset);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[StageConfigEditorWindow] Saved all PVE stages to local assets.");
        }

        private void PushToServer()
        {
            // Sync all changes from local representations
            var syncReqs = new List<SyncStageReqRepresentation>();
            foreach (var s in _serverStages)
            {
                string jsonData = JsonUtility.ToJson(s);
                syncReqs.Add(new SyncStageReqRepresentation
                {
                    stage_id = s.stageId,
                    json_data = jsonData
                });
            }

            string postData = $"{{\"items\":{JsonHelper.ToJson(syncReqs)}}}";
            // Strip wrapper object structure to match Go API array input
            int startIdx = postData.IndexOf('[');
            int endIdx = postData.LastIndexOf(']');
            if (startIdx >= 0 && endIdx >= 0)
            {
                postData = postData.Substring(startIdx, endIdx - startIdx + 1);
            }

            var request = new UnityWebRequest(API_SYNC_STAGES, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(postData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var asyncOp = request.SendWebRequest();
            asyncOp.completed += (op) =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[StageConfigEditorWindow] Successfully pushed & synced stages to Go Server.");
                    PullFromServer();
                }
                else
                {
                    Debug.LogError($"[StageConfigEditorWindow] Failed to push to server: {request.error}\nResponse: {request.downloadHandler.text}");
                }
                request.Dispose();
            };
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public List<T> items;
        }

        [System.Serializable]
        private class StageConfigDBRepresentation
        {
            public string stage_id;
            public string json_data;
            public string updated_at;
        }

        [System.Serializable]
        private class SyncStageReqRepresentation
        {
            public string stage_id;
            public string json_data;
        }

        [System.Serializable]
        private class StageDataRepresentation
        {
            public string stageId;
            public string stageName;
            public string description;
            public int recommendPower;
            public int staminaCost;
            public string combatSceneName;
            public List<MonsterConfigRepresentation> enemiesConfig;
        }

        [System.Serializable]
        private class MonsterConfigRepresentation
        {
            public string monsterId;
            public string name;
            public int level;
            public int maxHP;
            public int attack;
            public int defense;
            public int speed;
            public bool isBoss;
            public string prefabAddress;
        }

        private static class JsonHelper
        {
            public static string ToJson<T>(List<T> list)
            {
                Wrapper<T> wrapper = new Wrapper<T> { items = list };
                return JsonUtility.ToJson(wrapper);
            }
        }
    }
}
