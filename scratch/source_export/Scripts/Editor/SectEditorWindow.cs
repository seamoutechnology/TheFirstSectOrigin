using UnityEngine;
using UnityEditor;
using GameClient.Managers;

namespace GameClient.Editor
{
    public class SectEditorWindow : EditorWindow
    {
        [MenuItem("TFSO/Sect Debugger")]
        public static void ShowWindow()
        {
            GetWindow<SectEditorWindow>("Sect Debugger");
        }

        private void OnGUI()
        {
            GUILayout.Label("Sect Realtime Editor", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use this tool.", MessageType.Warning);
                return;
            }

            GameContext.SectLevel = EditorGUILayout.IntSlider("Sect Level", GameContext.SectLevel, 1, 100);
            GameContext.SectReputation = EditorGUILayout.IntField("Reputation", GameContext.SectReputation);
            GameContext.SectAlignment = EditorGUILayout.IntSlider("Alignment (Good/Evil)", GameContext.SectAlignment, -100, 100);

            EditorGUILayout.Space();
            GUILayout.Label("Current Status:", EditorStyles.boldLabel);
            GUILayout.Label($"Max Capacity: {GameContext.MaxDiscipleCapacity}");
            
            if (GameContext.IsGood)
                GUILayout.Label("Alignment: GOOD (Chính Phái)", EditorStyles.label);
            else if (GameContext.IsEvil)
                GUILayout.Label("Alignment: EVIL (Tà Tu)", EditorStyles.label);
            else
                GUILayout.Label("Alignment: NEUTRAL (Trung Lập)", EditorStyles.label);

            if (GUILayout.Button("Force Refresh Sect Environment"))
            {
                Debug.Log($"[SectEditor] Applied SectLevel = {GameContext.SectLevel}, Capacity = {GameContext.MaxDiscipleCapacity}");
            }
        }
    }
}
