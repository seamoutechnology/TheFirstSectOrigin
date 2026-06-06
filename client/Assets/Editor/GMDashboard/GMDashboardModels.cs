using UnityEngine;

namespace GameClient.Editor.GMDashboard
{
    // --- Player Models ---
    [System.Serializable]
    public class GMUserInfo
    {
        public long user_id;
        public string email;
        public string sect_name;
        public int level;
        public int money;
    }

    [System.Serializable]
    public class GMUserItem
    {
        public long id;
        public long user_id;
        public string item_code;
        public int quantity;
    }

    [System.Serializable]
    public class GMUserListItem
    {
        public long user_id;
        public string email;
        public string nickname;
        public int level;
    }

    [System.Serializable]
    public class GMUserListResponse
    {
        public int total;
        public int page;
        public GMUserListItem[] data;
    }

    // --- Notice Models ---
    [System.Serializable]
    public class GMAnnouncementData
    {
        public int id;
        public string type;
        public string title;
        public string content;
        public string start_at;
        public string end_at;
        public bool is_active;
    }

    // --- Item Config Models ---
    [System.Serializable]
    public class GMItemConfigData
    {
        public string item_code;
        public string name_key;
        public string type;
        public string rarity;
        public string icon;
        public string desc_key;
        public int max_stack;
        public string sources;
        public string effects;
    }

    [System.Serializable]
    public class GMItemEffect
    {
        public string effect_code;
        public float value;
        public float min_value;
        public float max_value;
    }

    // --- Effect Config Models ---
    [System.Serializable]
    public class GMEffectConfigData
    {
        public string effect_code;
        public string name_key;
        public string desc_key;
        public string effect_type;
        public string value_type;
        public float min_value;
        public float max_value;
    }

    // --- Server Models ---
    [System.Serializable]
    public class GMZoneDB
    {
        public int id;
        public string name;
        public string gateway_url;
        public string status;
        public bool is_active;
    }

    // --- Helpers ---
    public static class GMJsonHelper
    {
        public static T[] FromJson<T>(string json)
        {
            string newJson = "{ \"array\": " + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
            return wrapper.array;
        }

        [System.Serializable]
        private class Wrapper<T>
        {
            public T[] array;
        }
    }
}
