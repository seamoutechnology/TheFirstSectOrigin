using UnityEngine;
using UnityEditor;

namespace GameClient.Editor.GMDashboard
{
    public class GMDashboardWindow : EditorWindow
    {
        private int selectedTab = 0;
        private string[] tabs = { "Nhân vật", "Items", "Effects", "Features", "Missions", "Notices", "Servers", "Settings" };

        private GMPlayerTab      playerTab;
        private GMItemConfigTab  itemTab;
        private GMEffectConfigTab effectTab;
        private GMFeatureConfigTab featureTab;
        private GMMissionConfigTab missionTab;
        private GMNoticeTab      noticeTab;
        private GMServerTab      serverTab;
        private GMGameConfigTab  configTab;

        [MenuItem("Tools/GM Dashboard (Unified)")]
        public static void ShowWindow()
        {
            GetWindow<GMDashboardWindow>("GM Dashboard");
        }

        private void OnEnable()
        {
            playerTab  = new GMPlayerTab(this);
            itemTab    = new GMItemConfigTab(this);
            effectTab  = new GMEffectConfigTab(this);
            featureTab = new GMFeatureConfigTab(this);
            missionTab = new GMMissionConfigTab(this);
            noticeTab  = new GMNoticeTab(this);
            serverTab  = new GMServerTab(this);
            configTab  = new GMGameConfigTab(this);

            playerTab.OnEnable();
            itemTab.OnEnable();
            effectTab.OnEnable();
            featureTab.OnEnable();
            missionTab.OnEnable();
            noticeTab.OnEnable();
            serverTab.OnEnable();
            configTab.OnEnable();

            // Subscribe to connection changes so we repaint automatically
            GMDashboardConfig.OnStatusChanged += Repaint;
            EditorApplication.update += OnEditorTick;

            // Do an immediate ping
            GMDashboardConfig.ForceRefresh();
        }

        private void OnDisable()
        {
            GMDashboardConfig.OnStatusChanged -= Repaint;
            EditorApplication.update -= OnEditorTick;
        }

        private void OnEditorTick()
        {
            GMDashboardConfig.Tick();
        }

        private void OnGUI()
        {
            GUILayout.Space(6);

            // ── Global status bar (shown on every tab) ──────────────────
            GMDashboardConfig.DrawStatusBar();

            // ── Tab toolbar ─────────────────────────────────────────────
            EditorGUI.BeginChangeCheck();
            selectedTab = GUILayout.Toolbar(selectedTab, tabs, GUILayout.Height(28));
            if (EditorGUI.EndChangeCheck())
            {
                GUI.FocusControl(null);
            }

            GUILayout.Space(8);

            // ── Tab content ─────────────────────────────────────────────
            switch (selectedTab)
            {
                case 0: playerTab.OnGUI();  break;
                case 1: itemTab.OnGUI();    break;
                case 2: effectTab.OnGUI();  break;
                case 3: featureTab.OnGUI(); break;
                case 4: missionTab.OnGUI(); break;
                case 5: noticeTab.OnGUI();  break;
                case 6: serverTab.OnGUI();  break;
                case 7: configTab.OnGUI();  break;
            }
        }
    }
}
