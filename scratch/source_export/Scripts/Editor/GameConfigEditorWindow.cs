using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using GameClient.Managers;
using GameClient.Gameplay.Heroes;
using GameClient.Gameplay.BaseBuilder;

namespace GameClient.EditorTools
{
    public class GameConfigEditorWindow : EditorWindow
    {
        private enum Tab
        {
            Buildings,
            Characters,
            Items
        }

        private Tab currentTab = Tab.Buildings;
        private Vector2 scrollPos;
        
        private ServerConfigData _serverData = new ServerConfigData();

        private const string MOCK_FILE_PATH = "Assets/StreamingAssets/MockServerData.json";

        [MenuItem("Tools/Game Config Editor")]
        public static void ShowWindow()
        {
            GetWindow<GameConfigEditorWindow>("Game Configs");
        }

        private void OnEnable()
        {
            LoadFromServerMock();
        }

        private void LoadFromServerMock()
        {
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }

            if (File.Exists(MOCK_FILE_PATH))
            {
                string json = File.ReadAllText(MOCK_FILE_PATH);
                _serverData = JsonUtility.FromJson<ServerConfigData>(json);
            }
            else
            {
                _serverData = new ServerConfigData();
            }

            if (_serverData.buildings == null) _serverData.buildings = new List<BuildingConfigData>();
            if (_serverData.heroes == null) _serverData.heroes = new List<HeroConfigData>();
            if (_serverData.items == null) _serverData.items = new List<ItemConfigData>();
        }

        private void SaveToServerMock()
        {
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }

            string json = JsonUtility.ToJson(_serverData, true);
            File.WriteAllText(MOCK_FILE_PATH, json);
            AssetDatabase.Refresh();
            Debug.Log($"[GameConfigEditor] Đã lưu dữ liệu giả lập Server tại: {MOCK_FILE_PATH}");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            
            if (GUILayout.Toggle(currentTab == Tab.Buildings, "Buildings", "Button", GUILayout.Height(30)))
            {
                currentTab = Tab.Buildings;
                GUI.FocusControl(null);
            }
            if (GUILayout.Toggle(currentTab == Tab.Characters, "Characters", "Button", GUILayout.Height(30)))
            {
                currentTab = Tab.Characters;
                GUI.FocusControl(null);
            }
            if (GUILayout.Toggle(currentTab == Tab.Items, "Items/Chests", "Button", GUILayout.Height(30)))
            {
                currentTab = Tab.Items;
                GUI.FocusControl(null);
            }

            GUILayout.FlexibleSpace();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Save To Server (Mock)", GUILayout.Height(30), GUILayout.Width(150)))
            {
                SaveToServerMock();
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            if (currentTab == Tab.Buildings)
            {
                DrawBuildingsTab();
            }
            else if (currentTab == Tab.Characters)
            {
                DrawCharactersTab();
            }
            else if (currentTab == Tab.Items)
            {
                DrawItemsTab();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawBuildingsTab()
        {
            if (GUILayout.Button("Add New Building", GUILayout.Height(25)))
            {
                _serverData.buildings.Add(new BuildingConfigData { BuildingID = "b_new", BuildingNameKey = "New Building", SizeX = 2, SizeY = 2 });
            }

            EditorGUILayout.Space();

            for (int i = 0; i < _serverData.buildings.Count; i++)
            {
                var building = _serverData.buildings[i];
                
                EditorGUILayout.BeginVertical("box");
                
                EditorGUILayout.BeginHorizontal();
                building.BuildingNameKey = EditorGUILayout.TextField(building.BuildingNameKey, EditorStyles.boldLabel);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    _serverData.buildings.RemoveAt(i);
                    i--;
                    continue;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                building.BuildingID = EditorGUILayout.TextField("ID", building.BuildingID);
                building.Type = EditorGUILayout.Popup("Type", building.Type, new string[] { "MainHall", "Resource", "Military", "Decoration" });
                building.SizeX = EditorGUILayout.IntField("Size X", building.SizeX);
                building.SizeY = EditorGUILayout.IntField("Size Y", building.SizeY);
                building.RequiredReputation = EditorGUILayout.IntField("Req. Reputation", building.RequiredReputation);
                building.PrefabAddress = EditorGUILayout.TextField("Prefab Address", building.PrefabAddress);
                EditorGUI.indentLevel--;
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }

        private void DrawCharactersTab()
        {
            if (GUILayout.Button("Add New Character", GUILayout.Height(25)))
            {
                _serverData.heroes.Add(new HeroConfigData { heroId = System.DateTime.Now.Ticks % 10000, heroName = "New Hero", basePower = 100 });
            }

            EditorGUILayout.Space();

            for (int i = 0; i < _serverData.heroes.Count; i++)
            {
                var hero = _serverData.heroes[i];
                
                EditorGUILayout.BeginVertical("box");
                
                EditorGUILayout.BeginHorizontal();
                hero.heroName = EditorGUILayout.TextField(hero.heroName, EditorStyles.boldLabel);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    _serverData.heroes.RemoveAt(i);
                    i--;
                    continue;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                hero.heroId = EditorGUILayout.LongField("Hero ID", hero.heroId);
                
                EditorGUILayout.LabelField("Description");
                hero.description = EditorGUILayout.TextArea(hero.description, GUILayout.Height(40));
                
                hero.rarity = EditorGUILayout.TextField("Rarity", hero.rarity);
                hero.basePower = EditorGUILayout.FloatField("Base Power", hero.basePower);
                hero.prefabAddress = EditorGUILayout.TextField("Prefab Address", hero.prefabAddress);
                hero.iconAddress = EditorGUILayout.TextField("Icon Address", hero.iconAddress);
                EditorGUI.indentLevel--;
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }
        private void DrawItemsTab()
        {
            if (GUILayout.Button("Add New Item/Chest", GUILayout.Height(25)))
            {
                _serverData.items.Add(new ItemConfigData { itemId = "item_new", itemName = "New Item", description = "..." });
            }

            EditorGUILayout.Space();

            for (int i = 0; i < _serverData.items.Count; i++)
            {
                var item = _serverData.items[i];
                
                EditorGUILayout.BeginVertical("box");
                
                EditorGUILayout.BeginHorizontal();
                item.itemName = EditorGUILayout.TextField(item.itemName, EditorStyles.boldLabel);
                if (GUILayout.Button("X", GUILayout.Width(20)))
                {
                    _serverData.items.RemoveAt(i);
                    i--;
                    continue;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.indentLevel++;
                item.itemId = EditorGUILayout.TextField("Item ID", item.itemId);
                
                EditorGUILayout.LabelField("Description");
                item.description = EditorGUILayout.TextArea(item.description, GUILayout.Height(40));
                
                item.maxStack = EditorGUILayout.IntField("Max Stack", item.maxStack);
                EditorGUI.indentLevel--;
                
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
        }
    }
}
