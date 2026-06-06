using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using GameClient.Gameplay.Heroes;
using System.IO;

namespace GameClient.EditorTools
{
    public class HeroConfigEditorWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private List<HeroConfig> heroConfigs = new List<HeroConfig>();
        private HeroConfig selectedHero;
        
        private const string DATA_PATH = "Assets/GameData/Heroes";

        [MenuItem("Tools/Hero Manager")]
        public static void ShowWindow()
        {
            GetWindow<HeroConfigEditorWindow>("Hero Manager");
        }

        private void OnEnable()
        {
            LoadAllHeroes();
        }

        private void LoadAllHeroes()
        {
            heroConfigs.Clear();
            if (!Directory.Exists(DATA_PATH))
            {
                Directory.CreateDirectory(DATA_PATH);
                AssetDatabase.Refresh();
            }

            string[] guids = AssetDatabase.FindAssets("t:HeroConfig", new[] { DATA_PATH });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                HeroConfig config = AssetDatabase.LoadAssetAtPath<HeroConfig>(path);
                if (config != null)
                {
                    heroConfigs.Add(config);
                }
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();

            DrawLeftPanel();

            DrawRightPanel();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.Width(250), GUILayout.ExpandHeight(true));
            
            GUILayout.Label("Heroes List", EditorStyles.boldLabel);

            if (GUILayout.Button("Create New Hero", GUILayout.Height(30)))
            {
                CreateNewHero();
            }

            GUILayout.Space(10);

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            
            for (int i = 0; i < heroConfigs.Count; i++)
            {
                var hero = heroConfigs[i];
                if (hero == null) continue;

                GUI.color = selectedHero == hero ? Color.green : Color.white;
                
                if (GUILayout.Button($"{hero.heroId} - {hero.heroName}", GUILayout.Height(25)))
                {
                    selectedHero = hero;
                    GUI.FocusControl(null); // Clear focus when selecting new item
                }
                
                GUI.color = Color.white;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (selectedHero == null)
            {
                GUILayout.Label("Select a hero from the list to edit or create a new one.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                GUILayout.Label($"Editing: {selectedHero.heroName}", EditorStyles.boldLabel);
                EditorGUILayout.Space();

                EditorGUI.BeginChangeCheck();

                selectedHero.heroId = EditorGUILayout.LongField("Hero ID", selectedHero.heroId);
                selectedHero.heroName = EditorGUILayout.TextField("Hero Name", selectedHero.heroName);
                
                EditorGUILayout.LabelField("Description");
                selectedHero.description = EditorGUILayout.TextArea(selectedHero.description, GUILayout.Height(60));

                EditorGUILayout.Space();
                GUILayout.Label("Visuals", EditorStyles.boldLabel);
                selectedHero.prefabAddress = EditorGUILayout.TextField("Prefab Address", selectedHero.prefabAddress);
                selectedHero.iconAddress = EditorGUILayout.TextField("Icon Address", selectedHero.iconAddress);

                EditorGUILayout.Space();
                GUILayout.Label("Stats", EditorStyles.boldLabel);
                selectedHero.rarity = EditorGUILayout.TextField("Rarity", selectedHero.rarity);
                selectedHero.basePower = EditorGUILayout.FloatField("Base Power", selectedHero.basePower);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(selectedHero);
                }

                GUILayout.FlexibleSpace();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Delete Hero", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("Delete Hero", $"Are you sure you want to delete {selectedHero.heroName}?", "Yes", "No"))
                    {
                        string path = AssetDatabase.GetAssetPath(selectedHero);
                        AssetDatabase.DeleteAsset(path);
                        selectedHero = null;
                        LoadAllHeroes();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void CreateNewHero()
        {
            HeroConfig newHero = CreateInstance<HeroConfig>();
            newHero.heroId = GetNextAvailableId();
            newHero.heroName = "New Hero";

            string path = $"{DATA_PATH}/Hero_{newHero.heroId}.asset";
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(path);

            AssetDatabase.CreateAsset(newHero, uniquePath);
            AssetDatabase.SaveAssets();

            LoadAllHeroes();
            selectedHero = newHero;
        }

        private long GetNextAvailableId()
        {
            long maxId = 100;
            foreach (var hero in heroConfigs)
            {
                if (hero.heroId >= maxId)
                {
                    maxId = hero.heroId + 1;
                }
            }
            return maxId;
        }
    }
}
