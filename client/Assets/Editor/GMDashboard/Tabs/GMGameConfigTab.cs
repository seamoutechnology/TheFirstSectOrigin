using UnityEngine;
using UnityEditor;
using GameClient.Core;
using System.IO;
using System.Linq;
using UnityEngine.Localization.Settings;

namespace GameClient.Editor.GMDashboard
{
    public class GMGameConfigTab
    {
        private EditorWindow window;
        private GameSettings _settings;
        private Vector2 _scrollPos;

        public GMGameConfigTab(EditorWindow window)
        {
            this.window = window;
        }

        public void OnEnable()
        {
            LoadSettings();
        }

        private void LoadSettings()
        {
            _settings = Resources.Load<GameSettings>("GameSettings");
            if (_settings == null)
            {
                _settings = ScriptableObject.CreateInstance<GameSettings>();
                string path = "Assets/Resources";
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                AssetDatabase.CreateAsset(_settings, path + "/GameSettings.asset");
                AssetDatabase.SaveAssets();
            }
        }

        public void OnGUI()
        {
            if (_settings == null) return;

            EditorGUILayout.BeginVertical(new GUIStyle { padding = new RectOffset(15, 15, 15, 15) });
            
            GUILayout.Label("The First Sect Origin - Config Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space(10);

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // --- GM DASHBOARD CONNECTION ---
            EditorGUILayout.LabelField("GM DASHBOARD", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            string currentUrl = GMDashboardConfig.AdminUrl;
            string newUrl = EditorGUILayout.TextField("Admin URL", currentUrl);
            if (newUrl != currentUrl)
            {
                GMDashboardConfig.AdminUrl = newUrl;
                GMDashboardConfig.ForceRefresh();
            }

            // Status badge
            var statusBadge = GMDashboardConfig.Status switch
            {
                GMDashboardConfig.ConnectionStatus.Online   => (badge: "● ONLINE",   color: "#00e676"),
                GMDashboardConfig.ConnectionStatus.Offline  => (badge: "● OFFLINE",  color: "#ff5252"),
                GMDashboardConfig.ConnectionStatus.Checking => (badge: "◌ CHECKING", color: "#ffab40"),
                _                                           => (badge: "◌ UNKNOWN",  color: "#90a4ae")
            };
            GUIStyle badgeStyle = new GUIStyle(EditorStyles.miniLabel) { richText = true };
            GUILayout.Label($"<color={statusBadge.color}>{statusBadge.badge}</color>", badgeStyle, GUILayout.Width(90));
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Test Connection", GUILayout.Width(140)))
            {
                GMDashboardConfig.ForceRefresh();
            }
            EditorGUILayout.HelpBox(
                "URL của Admin Server (gm-api). Tất cả các tab trong GM Dashboard đều dùng chung URL này.\n" +
                "Mặc định: http://localhost:8080/api/gm",
                MessageType.Info);
            EditorGUILayout.Space(10);

            // --- PHẦN SERVER ---
            EditorGUILayout.LabelField("MẠNG & SERVER", EditorStyles.boldLabel);
            _settings.apiBaseUrl = EditorGUILayout.TextField("API Base URL", _settings.apiBaseUrl);
            _settings.cdnUrl = EditorGUILayout.TextField("CDN URL", _settings.cdnUrl);
            _settings.gatewayAddr = EditorGUILayout.TextField("Gateway Addr", _settings.gatewayAddr);
            EditorGUILayout.Space(10);

            // --- PHẦN PHIÊN BẢN ---
            EditorGUILayout.LabelField("PHIÊN BẢN", EditorStyles.boldLabel);
            _settings.gameVersion = EditorGUILayout.TextField("Version", _settings.gameVersion);
            _settings.buildNumber = EditorGUILayout.IntField("Build Number", _settings.buildNumber);
            EditorGUILayout.Space(10);

            // --- PHẦN LOCALIZATION ---
            EditorGUILayout.LabelField("ĐA NGÔN NGỮ", EditorStyles.boldLabel);
            _settings.defaultLocaleTable = EditorGUILayout.TextField("Default Table", _settings.defaultLocaleTable);

            var locales = LocalizationSettings.AvailableLocales?.Locales;
            if (locales != null && locales.Count > 0)
            {
                string[] options = locales.Select(l => $"{l.LocaleName} ({l.Identifier.Code})").ToArray();
                int currentIndex = locales.FindIndex(l => l.Identifier.Code == _settings.defaultLocaleCode);
                if (currentIndex == -1) currentIndex = 0;

                int newIndex = EditorGUILayout.Popup("Default Language", currentIndex, options);
                _settings.defaultLocaleCode = locales[newIndex].Identifier.Code;
            }
            else
            {
                EditorGUILayout.HelpBox("Không tìm thấy Locales nào. Hãy tạo chúng trong Localization Tables window.", MessageType.Warning);
            }
            EditorGUILayout.Space(10);

            // --- PHẦN ÂM THANH ---
            EditorGUILayout.LabelField("ÂM THANH MẶC ĐỊNH", EditorStyles.boldLabel);
            _settings.defaultMasterVolume = EditorGUILayout.Slider("Master Volume", _settings.defaultMasterVolume, 0, 1);
            _settings.defaultMusicVolume = EditorGUILayout.Slider("Music Volume", _settings.defaultMusicVolume, 0, 1);
            _settings.defaultSfxVolume = EditorGUILayout.Slider("SFX Volume", _settings.defaultSfxVolume, 0, 1);
            _settings.defaultMasterMute = EditorGUILayout.Toggle("Mute All", _settings.defaultMasterMute);
            EditorGUILayout.Space(10);

            // --- PHẦN DEBUG ---
            EditorGUILayout.LabelField("DEBUG SETTINGS", EditorStyles.boldLabel);
            _settings.showDebugLog = EditorGUILayout.Toggle("Show Debug Logs", _settings.showDebugLog);
            _settings.bypassLogin = EditorGUILayout.Toggle("Bypass Login", _settings.bypassLogin);

            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(20);
            
            GUI.color = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("RESET ALL GAME DATA (PlayerPrefs)", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Xác nhận Reset", "Bạn có chắc chắn muốn xóa sạch PlayerPrefs? Việc này sẽ xóa Token, Tài khoản cũ và mọi cài đặt người dùng.", "Xóa ngay", "Hủy"))
                {
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.Save();
                    Debug.Log("<color=red>[TFSO Config] Đã xóa sạch toàn bộ dữ liệu game (PlayerPrefs)!</color>");
                }
            }
            GUI.color = Color.white;

            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("SAVE ALL SETTINGS", GUILayout.Height(40)))
            {
                EditorUtility.SetDirty(_settings);
                AssetDatabase.SaveAssets();
                Debug.Log("<color=green>[TFSO Config] Đã lưu mọi thay đổi!</color>");
            }

            EditorGUILayout.EndVertical();
        }
    }
}
