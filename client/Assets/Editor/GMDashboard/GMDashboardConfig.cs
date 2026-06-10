using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System;

namespace GameClient.Editor.GMDashboard
{
    /// <summary>
    /// Shared configuration and connection state for all GM Dashboard tabs.
    /// Persists adminUrl via EditorPrefs so it survives Unity restarts.
    /// </summary>
    public static class GMDashboardConfig
    {
        private const string PREF_KEY_ADMIN_URL = "GM_Dashboard_AdminUrl";
        private const string DEFAULT_ADMIN_URL   = "http://localhost:8080/api/gm";

        // ── Connection State ──────────────────────────────────────────────
        public enum ConnectionStatus { Unknown, Checking, Online, Offline }
        public static ConnectionStatus Status { get; private set; } = ConnectionStatus.Unknown;

        // Timestamp of the last ping so we only re-ping periodically
        private static double _lastCheckTime = -999;
        private const double CHECK_INTERVAL_SECONDS = 5.0;

        // Callback so windows can repaint when status changes
        public static event Action OnStatusChanged;

        // ── Admin URL ─────────────────────────────────────────────────────
        public static string AdminUrl
        {
            get => EditorPrefs.GetString(PREF_KEY_ADMIN_URL, DEFAULT_ADMIN_URL);
            set => EditorPrefs.SetString(PREF_KEY_ADMIN_URL, value);
        }

        /// <summary>Base GM API URL (no trailing slash)</summary>
        public static string GmApiUrl => AdminUrl.TrimEnd('/');

        // ── Periodic Ping ─────────────────────────────────────────────────
        /// <summary>
        /// Call this from EditorApplication.update (or similar) to do
        /// background connectivity checks.
        /// </summary>
        public static void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (now - _lastCheckTime < CHECK_INTERVAL_SECONDS) return;
            _lastCheckTime = now;
            PingServer();
        }

        public static void ForceRefresh()
        {
            _lastCheckTime = -999;
        }

        // ── Internal Ping ─────────────────────────────────────────────────
        private static UnityWebRequest _pingRequest;

        private static void PingServer()
        {
            // Abort any in-flight request
            _pingRequest?.Abort();

            string pingUrl = GmApiUrl + "/ping";
            _pingRequest = UnityWebRequest.Get(pingUrl);
            _pingRequest.timeout = 3;

            var op = _pingRequest.SendWebRequest();
            var prevStatus = Status;
            Status = ConnectionStatus.Checking;

            op.completed += _ =>
            {
                var req = _pingRequest;
                if (req == null) return;

                bool ok = req.result == UnityWebRequest.Result.Success;
                Status = ok ? ConnectionStatus.Online : ConnectionStatus.Offline;

                if (Status != prevStatus)
                    OnStatusChanged?.Invoke();
            };
        }

        // ── GUI Helpers ───────────────────────────────────────────────────

        // Reusable styles (lazily created)
        private static GUIStyle _barStyle;
        private static GUIStyle _labelStyle;

        /// <summary>
        /// Draws the connection status bar at the top of a tab.
        /// Returns true when the admin server is reachable (tabs should enable actions).
        /// </summary>
        public static bool DrawStatusBar()
        {
            // Ensure styles exist
            if (_barStyle == null)
            {
                _barStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(8, 8, 4, 4),
                    margin  = new RectOffset(0, 0, 0, 6)
                };
            }
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(EditorStyles.label)
                {
                    richText  = true,
                    fontStyle = FontStyle.Bold,
                    fontSize  = 11
                };
            }

            // Colour + icon
            var state = Status switch
            {
                ConnectionStatus.Online   => (icon: "●", label: "ONLINE",   colorHex: "#00e676"),
                ConnectionStatus.Offline  => (icon: "●", label: "OFFLINE",  colorHex: "#ff5252"),
                ConnectionStatus.Checking => (icon: "◌", label: "CHECKING", colorHex: "#ffab40"),
                _                         => (icon: "◌", label: "UNKNOWN",  colorHex: "#90a4ae")
            };

            bool isOnline = Status == ConnectionStatus.Online;

            using (new EditorGUILayout.HorizontalScope(_barStyle))
            {
                // Status dot + text
                GUILayout.Label(
                    $"<color={state.colorHex}>{state.icon}  Admin Server — {state.label}</color>  " +
                    $"<color=#b0bec5>{GMDashboardConfig.GmApiUrl}</color>",
                    _labelStyle);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Refresh", GUILayout.Width(65), GUILayout.Height(20)))
                {
                    GMDashboardConfig.ForceRefresh();
                }
            }

            if (!isOnline)
            {
                EditorGUILayout.HelpBox(
                    "Admin server chưa kết nối. Hãy vào tab \"Servers\" để khởi động, " +
                    "hoặc vào tab \"Settings\" để thay đổi Admin URL.",
                    MessageType.Warning);
            }

            return isOnline;
        }
    }
}
