using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using GameClient.Gameplay.Heroes;

namespace GameClient.Editor
{
    public class TraitConfigEditorWindow : EditorWindow
    {
        private const string API_GET_TRAITS = "http://localhost:8080/api/gm/traits";
        private const string API_SYNC_TRAITS = "http://localhost:8080/api/gm/traits/sync";
        private const string TRAITS_FOLDER_PATH = "Assets/Resources/GameData/Traits";

        private List<TraitDataRepresentation> _serverTraits = new List<TraitDataRepresentation>();
        private int _selectedTraitIndex = -1;
        private Vector2 _scrollPos;
        private Vector2 _detailScrollPos;

        [MenuItem("Tools/Disciple Personalities (Traits) Sync")]
        public static void ShowWindow()
        {
            var window = GetWindow<TraitConfigEditorWindow>();
            window.titleContent = new GUIContent("Personalities (Traits)");
            window.minSize = new Vector2(700, 500);
            window.Show();
        }

        private void OnEnable()
        {
            PullFromServer();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            // Left panel - list of traits
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(240), GUILayout.ExpandHeight(true));
            GUILayout.Label("Traits List", EditorStyles.boldLabel);

            if (GUILayout.Button("Pull from Server", GUILayout.Height(30)))
            {
                PullFromServer();
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            for (int i = 0; i < _serverTraits.Count; i++)
            {
                var trait = _serverTraits[i];
                string displayName = string.IsNullOrEmpty(trait.traitCode) ? "Unnamed Trait" : trait.traitCode;
                
                GUI.color = _selectedTraitIndex == i ? Color.green : Color.white;
                if (GUILayout.Button($"{displayName} (Weight: {trait.spawnWeight})", GUILayout.Height(25)))
                {
                    _selectedTraitIndex = i;
                    GUI.FocusControl(null);
                }
                GUI.color = Color.white;
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Create New Trait", GUILayout.Height(30)))
            {
                CreateNewLocalTrait();
            }
            EditorGUILayout.EndVertical();

            // Right panel - trait detail editor
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (_selectedTraitIndex >= 0 && _selectedTraitIndex < _serverTraits.Count)
            {
                _detailScrollPos = EditorGUILayout.BeginScrollView(_detailScrollPos);
                DrawTraitDetail(_serverTraits[_selectedTraitIndex]);
                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("Select a trait from the list or create a new one to start editing.", EditorStyles.centeredGreyMiniLabel);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            // Footer - sync buttons
            EditorGUILayout.BeginHorizontal(GUI.skin.box);
            if (GUILayout.Button("Save Local ScriptableObjects", GUILayout.Height(35)))
            {
                SaveAllLocalAssets();
            }
            if (GUILayout.Button("Push & Sync Traits to Go Server", GUILayout.Height(35)))
            {
                PushToServer();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTraitDetail(TraitDataRepresentation trait)
        {
            GUILayout.Label($"Editing Trait: {trait.traitCode}", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            trait.traitCode = EditorGUILayout.TextField("Trait Code", trait.traitCode);
            trait.nameKey = EditorGUILayout.TextField("Name Localization Key", trait.nameKey);
            trait.descriptionKey = EditorGUILayout.TextField("Description Localization Key", trait.descriptionKey);
            trait.spawnWeight = EditorGUILayout.IntField("Spawn Weight (on Server)", trait.spawnWeight);

            EditorGUILayout.Space();
            GUILayout.Label("Effects List", EditorStyles.boldLabel);

            if (trait.effects == null)
            {
                trait.effects = new List<TraitEffectRepresentation>();
            }

            for (int i = 0; i < trait.effects.Count; i++)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Effect #{i + 1}", EditorStyles.miniBoldLabel);
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    trait.effects.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();

                var effect = trait.effects[i];
                effect.effectCode = EditorGUILayout.TextField("Effect Code", effect.effectCode);
                effect.value = EditorGUILayout.FloatField("Value", effect.value);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Add Effect"))
            {
                trait.effects.Add(new TraitEffectRepresentation { effectCode = "NEW_EFFECT", value = 0f });
            }

            GUILayout.Space(20);
            if (GUILayout.Button("Delete Trait Template", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Delete Trait", $"Are you sure you want to delete {trait.traitCode}?", "Yes", "No"))
                {
                    _serverTraits.RemoveAt(_selectedTraitIndex);
                    _selectedTraitIndex = -1;
                }
            }
        }

        private void PullFromServer()
        {
            var request = UnityWebRequest.Get(API_GET_TRAITS);
            var asyncOp = request.SendWebRequest();

            asyncOp.completed += (op) =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    var dbList = JsonUtility.FromJson<Wrapper<TraitConfigDBRepresentation>>($"{{\"items\":{json}}}");
                    _serverTraits.Clear();
                    if (dbList != null && dbList.items != null)
                    {
                        foreach (var dbItem in dbList.items)
                        {
                            var trait = JsonUtility.FromJson<TraitDataRepresentation>(dbItem.json_data);
                            if (trait != null)
                            {
                                // Sync values stored at top level (weight)
                                trait.spawnWeight = dbItem.weight;
                                _serverTraits.Add(trait);
                            }
                        }
                    }
                    Repaint();
                    Debug.Log("[TraitConfigEditorWindow] Successfully pulled traits from Go Server.");
                }
                else
                {
                    Debug.LogError($"[TraitConfigEditorWindow] Failed to pull from server: {request.error}");
                }
                request.Dispose();
            };
        }

        private void CreateNewLocalTrait()
        {
            string newCode = $"trait_new_{_serverTraits.Count + 1}";
            _serverTraits.Add(new TraitDataRepresentation
            {
                traitCode = newCode,
                nameKey = $"trait_{newCode}_name",
                descriptionKey = $"trait_{newCode}_desc",
                spawnWeight = 100,
                effects = new List<TraitEffectRepresentation>()
            });
            _selectedTraitIndex = _serverTraits.Count - 1;
        }

        private void SaveAllLocalAssets()
        {
            if (!Directory.Exists(TRAITS_FOLDER_PATH))
            {
                Directory.CreateDirectory(TRAITS_FOLDER_PATH);
            }

            foreach (var traitRep in _serverTraits)
            {
                string assetPath = $"{TRAITS_FOLDER_PATH}/Trait_{traitRep.traitCode}.asset";
                TraitData asset = AssetDatabase.LoadAssetAtPath<TraitData>(assetPath);
                bool isNew = asset == null;

                if (isNew)
                {
                    asset = CreateInstance<TraitData>();
                }

                asset.traitCode = traitRep.traitCode;
                asset.nameKey = traitRep.nameKey;
                asset.descriptionKey = traitRep.descriptionKey;
                asset.spawnWeight = traitRep.spawnWeight;

                asset.effects = new List<TraitEffect>();
                if (traitRep.effects != null)
                {
                    foreach (var effRep in traitRep.effects)
                    {
                        asset.effects.Add(new TraitEffect
                        {
                            effectCode = effRep.effectCode,
                            value = effRep.value
                        });
                    }
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
            Debug.Log("[TraitConfigEditorWindow] Saved all traits to local assets.");
        }

        private void PushToServer()
        {
            var syncReqs = new List<SyncTraitReqRepresentation>();
            foreach (var t in _serverTraits)
            {
                string jsonData = JsonUtility.ToJson(t);
                syncReqs.Add(new SyncTraitReqRepresentation
                {
                    trait_code = t.traitCode,
                    weight = t.spawnWeight,
                    json_data = jsonData
                });
            }

            string postData = $"{{\"items\":{JsonHelper.ToJson(syncReqs)}}}";
            int startIdx = postData.IndexOf('[');
            int endIdx = postData.LastIndexOf(']');
            if (startIdx >= 0 && endIdx >= 0)
            {
                postData = postData.Substring(startIdx, endIdx - startIdx + 1);
            }

            var request = new UnityWebRequest(API_SYNC_TRAITS, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(postData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var asyncOp = request.SendWebRequest();
            asyncOp.completed += (op) =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[TraitConfigEditorWindow] Successfully pushed & synced traits to Go Server.");
                    PullFromServer();
                }
                else
                {
                    Debug.LogError($"[TraitConfigEditorWindow] Failed to push to server: {request.error}\nResponse: {request.downloadHandler.text}");
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
        private class TraitConfigDBRepresentation
        {
            public string trait_code;
            public int weight;
            public string json_data;
            public string updated_at;
        }

        [System.Serializable]
        private class SyncTraitReqRepresentation
        {
            public string trait_code;
            public int weight;
            public string json_data;
        }

        [System.Serializable]
        private class TraitDataRepresentation
        {
            public string traitCode;
            public string nameKey;
            public string descriptionKey;
            public int spawnWeight;
            public List<TraitEffectRepresentation> effects;
        }

        [System.Serializable]
        private class TraitEffectRepresentation
        {
            public string effectCode;
            public float value;
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
