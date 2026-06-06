namespace GameClient.Core
{
    public static class GameConstants
    {
        public static class Network
        {
            public const string API_BASE_URL = "http://localhost:8081";
            public const string GATEWAY_HOST = "localhost";
            public const int LOGIN_PORT = 50051;
            public const int GATEWAY_PORT = 50052;
            public const float REQUEST_TIMEOUT = 10f;
            public const int DEFAULT_PORT = 50051;
            public const string BEARER_PREFIX = "Bearer ";
        }

        public static class UI
        {
            public const float DEFAULT_TRANSITION_TIME = 0.5f;
            public const float LOADING_BAR_SPEED = 2.0f;
            public const float CLICK_VFX_DELAY = 0.6f;
            public const float CAMERA_ZOOM_SPEED = 0.5f;
            public const float DAMAGE_NUMBER_DURATION = 1.0f;
        }

        public static class Combat
        {
            public const int GRID_WIDTH = 4;
            public const int GRID_HEIGHT = 4;
            public const float TURN_DELAY = 1.5f;
            public const float COMBO_WAIT_TIME = 1.0f;
        }

        public static class PlayerPrefsKeys
        {
            public const string TOKEN = "TFSO_TOKEN";
            public const string LAST_ACCOUNT = "TFSO_LAST_ACCOUNT";
            public const string SETTINGS = "GameSettings";
        }

        public static class Audio
        {
            public const string SFX_CLICK = "SFX_UI_Click";
            public const string SFX_HOVER = "SFX_UI_Hover";
            public const string SFX_TYPE = "SFX_UI_Type";
            public const string BGM_LOBBY = "BGM_LOBBY";
            public const string BGM_LOCAL_BASE = "BGM_LOCAL_BASE";
            public const string BGM_WORLD = "BGM_WORLD";
            public const string BGM_DUNGEON = "BGM_DUNGEON";
        }

        public static class Locales
        {
            public const string MSG_CONNECTING = "ui_connecting_gateway";
            public const string MSG_NO_SERVER = "ui_no_server";
            public const string MSG_SELECT_SERVER = "ui_select_server";
            public const string ERR_AUTH_FAILED = "err_auth_failed";
            public const string WARN_18_PLUS = "msg_18plus_content";
            public const string WARN_180_MINS = "msg_180_mins_warning";
        }

        public static class LocaleTable
        {
            public const string UI_SYSTEM = "UI_System";
            public const string SKILL_DES = "Skill_Description";
            public const string STORY_DIALOGUE = "Story_Dialogue";
            public const string SERVER_NOTICE = "Server_Notice";
            public const string ITEM_EQUIPMENT = "Item_Equipment";
            public const string HERO_DATA = "Hero_Data";
            public const string BATTLE_COMBAT = "Battle_Combat";
        }

        public static class Addressables
        {
            public const string LABEL_PRELOAD = "Preload";
            public const string LABEL_HEROES = "Heroes";
            public const string PATH_LOGIN_PANEL = "Assets/Prefabs/UI/LoginPanel.prefab";
        }
    }
}
