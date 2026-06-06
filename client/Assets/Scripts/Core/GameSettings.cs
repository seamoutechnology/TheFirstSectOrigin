using UnityEngine;

namespace GameClient.Core
{
    [CreateAssetMenu(fileName = "GameSettings", menuName = "TFSO/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Mạng & Server")]
        public string apiBaseUrl = "http://192.168.0.111";
        public string cdnUrl = "http://192.168.0.111/cdn/";
        public string gatewayAddr = "192.168.0.111:50051";

        [Header("Phiên bản")]
        public string gameVersion = "1.0.0";
        public int buildNumber = 1;

        [Header("Localization")]
        public string defaultLocaleCode = "vi";
        public string defaultLocaleTable = GameClient.Core.GameConstants.LocaleTable.UI_SYSTEM;

        [Header("Âm thanh mặc định")]
        [Range(0, 1)] public float defaultMasterVolume = 1f;
        [Range(0, 1)] public float defaultMusicVolume = 0.8f;
        [Range(0, 1)] public float defaultSfxVolume = 1f;
        public bool defaultMasterMute = false;
        
        [Header("Debug")]
        public bool showDebugLog = true;
        public bool bypassLogin = false;

        private static GameSettings _instance;
        public static GameSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<GameSettings>("GameSettings");
                return _instance;
            }
        }
    }
}
