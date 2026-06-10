using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;
using GameClient.Gameplay.Heroes;

namespace GameClient.EditorTools
{
    public class HeroConfigEditorWindow : EditorWindow
    {
        private const string API_GET_HEROES = "http://localhost:8080/api/gm/heroes";
        private const string API_SYNC_HEROES = "http://localhost:8080/api/gm/heroes/sync";
        private const string DATA_PATH = "Assets/GameData/Heroes";

        private List<HeroTemplateDBRepresentation> _serverHeroes = new List<HeroTemplateDBRepresentation>();
        private int _selectedHeroIndex = -1;
        private Vector2 _scrollPos;

        [MenuItem("Tools/Hero Manager (Cloud Sync)")]
        public static void ShowWindow()
        {
            var window = GetWindow<HeroConfigEditorWindow>();
            window.titleContent = new GUIContent("Hero Manager");
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

            // Left panel - list of heroes
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(240), GUILayout.ExpandHeight(true));
            GUILayout.Label("Heroes List", EditorStyles.boldLabel);

            if (GUILayout.Button("Pull from Server", GUILayout.Height(30)))
            {
                PullFromServer();
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            for (int i = 0; i < _serverHeroes.Count; i++)
            {
                var hero = _serverHeroes[i];
                string displayName = string.IsNullOrEmpty(hero.name) ? hero.code : hero.name;
                string activeStatus = hero.is_active ? "Active" : "Inactive";
                
                GUI.color = _selectedHeroIndex == i ? Color.green : Color.white;
                if (GUILayout.Button($"[{hero.code}] {displayName} ({activeStatus})", GUILayout.Height(25)))
                {
                    _selectedHeroIndex = i;
                    GUI.FocusControl(null);
                }
                GUI.color = Color.white;
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Create New Hero", GUILayout.Height(30)))
            {
                CreateNewLocalHero();
            }
            EditorGUILayout.EndVertical();

            // Right panel - hero detail editor
            EditorGUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (_selectedHeroIndex >= 0 && _selectedHeroIndex < _serverHeroes.Count)
            {
                DrawHeroDetail(_serverHeroes[_selectedHeroIndex]);
            }
            else
            {
                GUILayout.Label("Select a hero from the list or create a new one to start editing.", EditorStyles.centeredGreyMiniLabel);
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

        private void DrawHeroDetail(HeroTemplateDBRepresentation hero)
        {
            GUILayout.Label($"Editing Hero Template: {hero.code}", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            hero.code = EditorGUILayout.TextField("Hero Code", hero.code);
            hero.name = EditorGUILayout.TextField("Hero Name", hero.name);
            hero.rarity = EditorGUILayout.TextField("Rarity (UR/SSR/SR/R)", hero.rarity);
            hero.element = EditorGUILayout.TextField("Element (FIRE/WATER/WOOD)", hero.element);
            hero.role = EditorGUILayout.TextField("Role (WARRIOR/TANK/HEALER)", hero.role);
            hero.is_active = EditorGUILayout.Toggle("Is Active", hero.is_active);

            EditorGUILayout.Space();
            GUILayout.Label("Base Stats", EditorStyles.boldLabel);
            hero.base_hp = EditorGUILayout.IntField("Base HP", hero.base_hp);
            hero.base_atk = EditorGUILayout.IntField("Base ATK", hero.base_atk);
            hero.base_def = EditorGUILayout.IntField("Base DEF", hero.base_def);
            hero.base_speed = EditorGUILayout.IntField("Base Speed", hero.base_speed);
            hero.gacha_weight = EditorGUILayout.IntField("Gacha Weight", hero.gacha_weight);

            EditorGUILayout.Space();
            GUILayout.Label("Local/Client Metadata (Saved to ScriptableObject)", EditorStyles.boldLabel);
            hero.description = EditorGUILayout.TextField("Description", hero.description);
            hero.prefabAddress = EditorGUILayout.TextField("Prefab Address", hero.prefabAddress);
            hero.iconAddress = EditorGUILayout.TextField("Icon Address", hero.iconAddress);
            hero.maxLifespan = EditorGUILayout.IntField("Max Lifespan", hero.maxLifespan <= 0 ? 100 : hero.maxLifespan);
            hero.baseLoyalty = EditorGUILayout.IntField("Base Loyalty", hero.baseLoyalty <= 0 ? 80 : hero.baseLoyalty);
            hero.basePower = EditorGUILayout.FloatField("Base Power", hero.basePower <= 0f ? 100f : hero.basePower);
            hero.heroId = EditorGUILayout.LongField("Client Hero ID", hero.heroId <= 0 ? GetNextAvailableId() : hero.heroId);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Delete Hero Template", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Delete Hero Template", $"Are you sure you want to delete {hero.code}?", "Yes", "No"))
                {
                    _serverHeroes.RemoveAt(_selectedHeroIndex);
                    _selectedHeroIndex = -1;
                }
            }
        }

        private void PullFromServer()
        {
            var request = UnityWebRequest.Get(API_GET_HEROES);
            var asyncOp = request.SendWebRequest();

            asyncOp.completed += (op) =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string json = request.downloadHandler.text;
                    var dbList = JsonUtility.FromJson<Wrapper<HeroTemplateDBRepresentation>>($"{{\"items\":{json}}}");
                    _serverHeroes.Clear();
                    if (dbList != null && dbList.items != null)
                    {
                        foreach (var dbItem in dbList.items)
                        {
                            // Load existing ScriptableObject to sync metadata from client file if exists
                            HeroConfig localConfig = FindLocalConfigByCode(dbItem.code);
                            if (localConfig != null)
                            {
                                dbItem.description = localConfig.description;
                                dbItem.prefabAddress = localConfig.prefabAddress;
                                dbItem.iconAddress = localConfig.iconAddress;
                                dbItem.maxLifespan = localConfig.MaxLifespan;
                                dbItem.baseLoyalty = localConfig.BaseLoyalty;
                                dbItem.basePower = localConfig.basePower;
                                dbItem.heroId = localConfig.heroId;
                            }
                            _serverHeroes.Add(dbItem);
                        }
                    }
                    Repaint();
                    Debug.Log("[HeroConfigEditorWindow] Successfully pulled hero templates from Go Server.");
                }
                else
                {
                    Debug.LogError($"[HeroConfigEditorWindow] Failed to pull from server: {request.error}");
                }
                request.Dispose();
            };
        }

        private void CreateNewLocalHero()
        {
            string nextCode = $"HERO_{_serverHeroes.Count + 1:00}";
            var newHero = new HeroTemplateDBRepresentation
            {
                code = nextCode,
                name = "New Hero",
                rarity = "R",
                element = "FIRE",
                role = "WARRIOR",
                base_hp = 100,
                base_atk = 10,
                base_def = 5,
                base_speed = 10,
                gacha_weight = 100,
                is_active = true,
                maxLifespan = 100,
                baseLoyalty = 80,
                basePower = 100f,
                heroId = GetNextAvailableId()
            };
            _serverHeroes.Add(newHero);
            _selectedHeroIndex = _serverHeroes.Count - 1;
        }

        private void SaveAllLocalAssets()
        {
            if (!Directory.Exists(DATA_PATH))
            {
                Directory.CreateDirectory(DATA_PATH);
            }

            foreach (var heroRep in _serverHeroes)
            {
                string assetPath = $"{DATA_PATH}/Hero_{heroRep.code}.asset";
                HeroConfig asset = AssetDatabase.LoadAssetAtPath<HeroConfig>(assetPath);
                
                // If code changed, check if there's any file named after the old code or find by matching ID
                if (asset == null)
                {
                    asset = FindLocalConfigByCode(heroRep.code);
                }

                bool isNew = asset == null;
                if (isNew)
                {
                    asset = CreateInstance<HeroConfig>();
                }

                asset.code = heroRep.code;
                asset.heroName = heroRep.name;
                asset.rarity = heroRep.rarity;
                asset.element = heroRep.element;
                asset.role = heroRep.role;
                asset.baseHp = heroRep.base_hp;
                asset.baseAtk = heroRep.base_atk;
                asset.baseDef = heroRep.base_def;
                asset.baseSpeed = heroRep.base_speed;
                asset.gachaWeight = heroRep.gacha_weight;
                asset.isActive = heroRep.is_active;

                asset.description = heroRep.description;
                asset.prefabAddress = heroRep.prefabAddress;
                asset.iconAddress = heroRep.iconAddress;
                asset.MaxLifespan = heroRep.maxLifespan;
                asset.BaseLoyalty = heroRep.baseLoyalty;
                asset.basePower = heroRep.basePower;
                asset.heroId = heroRep.heroId;

                if (isNew)
                {
                    string uniquePath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
                    AssetDatabase.CreateAsset(asset, uniquePath);
                }
                else
                {
                    EditorUtility.SetDirty(asset);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[HeroConfigEditorWindow] Saved all hero templates to local assets.");
        }

        private void PushToServer()
        {
            var syncReqs = new List<HeroTemplateDBRepresentation>();
            foreach (var h in _serverHeroes)
            {
                syncReqs.Add(h);
            }

            string postData = $"{{\"items\":{JsonHelper.ToJson(syncReqs)}}}";
            // Strip wrapper object structure to match Go API array input
            int startIdx = postData.IndexOf('[');
            int endIdx = postData.LastIndexOf(']');
            if (startIdx >= 0 && endIdx >= 0)
            {
                postData = postData.Substring(startIdx, endIdx - startIdx + 1);
            }

            var request = new UnityWebRequest(API_SYNC_HEROES, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(postData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var asyncOp = request.SendWebRequest();
            asyncOp.completed += (op) =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("[HeroConfigEditorWindow] Successfully pushed & synced hero templates to Go Server.");
                    PullFromServer();
                }
                else
                {
                    Debug.LogError($"[HeroConfigEditorWindow] Failed to push to server: {request.error}\nResponse: {request.downloadHandler.text}");
                }
                request.Dispose();
            };
        }

        private HeroConfig FindLocalConfigByCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            string[] guids = AssetDatabase.FindAssets("t:HeroConfig", new[] { DATA_PATH });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                HeroConfig config = AssetDatabase.LoadAssetAtPath<HeroConfig>(path);
                if (config != null && config.code == code)
                {
                    return config;
                }
            }
            return null;
        }

        private long GetNextAvailableId()
        {
            long maxId = 100;
            foreach (var h in _serverHeroes)
            {
                if (h.heroId >= maxId)
                {
                    maxId = h.heroId + 1;
                }
            }
            return maxId;
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public List<T> items;
        }

        [System.Serializable]
        private class HeroTemplateDBRepresentation
        {
            public string code;
            public string name;
            public string rarity;
            public string element;
            public string role;
            public int base_hp;
            public int base_atk;
            public int base_def;
            public int base_speed;
            public int gacha_weight;
            public bool is_active;

            // Extra client-only metadata saved to ScriptableObjects
            public string description;
            public string prefabAddress;
            public string iconAddress;
            public int maxLifespan;
            public int baseLoyalty;
            public float basePower;
            public long heroId;
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
