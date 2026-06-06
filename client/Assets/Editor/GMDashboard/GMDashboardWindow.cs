using UnityEngine;
using UnityEditor;

namespace GameClient.Editor.GMDashboard
{
    public class GMDashboardWindow : EditorWindow
    {
        private int selectedTab = 0;
        private string[] tabs = { "Nhân vật", "Items", "Effects", "Notices", "Servers", "Settings" };

        private GMPlayerTab playerTab;
        private GMItemConfigTab itemTab;
        private GMEffectConfigTab effectTab;
        private GMNoticeTab noticeTab;
        private GMServerTab serverTab;
        private GMGameConfigTab configTab;

        [MenuItem("Tools/GM Dashboard (Unified)")]
        public static void ShowWindow()
        {
            GetWindow<GMDashboardWindow>("GM Dashboard");
        }

        private void OnEnable()
        {
            playerTab = new GMPlayerTab(this);
            itemTab = new GMItemConfigTab(this);
            effectTab = new GMEffectConfigTab(this);
            noticeTab = new GMNoticeTab(this);
            serverTab = new GMServerTab(this);
            configTab = new GMGameConfigTab(this);

            playerTab.OnEnable();
            itemTab.OnEnable();
            effectTab.OnEnable();
            noticeTab.OnEnable();
            serverTab.OnEnable();
            configTab.OnEnable();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            
            EditorGUI.BeginChangeCheck();
            selectedTab = GUILayout.Toolbar(selectedTab, tabs, GUILayout.Height(30));
            if (EditorGUI.EndChangeCheck())
            {
                GUI.FocusControl(null); // Clear focus when switching tabs
            }

            GUILayout.Space(10);

            switch (selectedTab)
            {
                case 0:
                    playerTab.OnGUI();
                    break;
                case 1:
                    itemTab.OnGUI();
                    break;
                case 2:
                    effectTab.OnGUI();
                    break;
                case 3:
                    noticeTab.OnGUI();
                    break;
                case 4:
                    serverTab.OnGUI();
                    break;
                case 5:
                    configTab.OnGUI();
                    break;
            }
        }
    }
}
