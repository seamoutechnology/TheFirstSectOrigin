using System;
using System.Threading.Tasks;
using UnityEngine;
using GameClient.Core;

namespace GameClient.Managers
{
    [Serializable]
    public class BuildingConfigData
    {
        public string BuildingID;
        public string BuildingNameKey;
        public int Type; // 0=MainHall, 1=Resource, 2=Military, 3=Decoration
        public int SizeX;
        public int SizeY;
        public string PrefabAddress;
        public int RequiredReputation;
    }

    [Serializable]
    public class HeroConfigData
    {
        public long heroId;
        public string heroName;
        public string description;
        public string prefabAddress;
        public string iconAddress;
        public string rarity;
        public float basePower;
    }

    [Serializable]
    public class ItemConfigData
    {
        public string itemId;
        public string itemName;
        public string description;
        public int maxStack = 99;
    }

    [Serializable]
    public class ServerConfigData
    {
        public int serverId;
        public int max_free_research;
        public System.Collections.Generic.List<BuildingConfigData> buildings = new System.Collections.Generic.List<BuildingConfigData>();
        public System.Collections.Generic.List<HeroConfigData> heroes = new System.Collections.Generic.List<HeroConfigData>();
        public System.Collections.Generic.List<ItemConfigData> items = new System.Collections.Generic.List<ItemConfigData>();
    }

    public class ServerConfigManager : Singleton<ServerConfigManager>
    {
        public ServerConfigData CurrentConfig { get; private set; }

        protected override void Awake()
        {
            base.Awake();
        }

        public async Task FetchConfigFromServer(int serverId)
        {
            Debug.Log($"[ServerConfig] Đang tải cấu hình cho Server {serverId}...");
            
            await Task.Delay(500);

            string mockFilePath = System.IO.Path.Combine(Application.streamingAssetsPath, "MockServerData.json");
            string mockJsonResponse = "";

            if (System.IO.File.Exists(mockFilePath))
            {
                mockJsonResponse = System.IO.File.ReadAllText(mockFilePath);
                CurrentConfig = JsonUtility.FromJson<ServerConfigData>(mockJsonResponse);
                if (CurrentConfig != null)
                {
                    CurrentConfig.serverId = serverId;
                }
            }
            else
            {
                Debug.LogWarning("[ServerConfig] Không tìm thấy MockServerData.json trong StreamingAssets. Dùng cấu hình mặc định.");
                CurrentConfig = new ServerConfigData { serverId = serverId, max_free_research = 1 };
            }
            
            Debug.Log($"[ServerConfig] Tải thành công! Server {serverId} cho phép mặc định {CurrentConfig.max_free_research} nhánh nghiên cứu. Loaded {CurrentConfig.buildings.Count} buildings, {CurrentConfig.heroes.Count} heroes.");
        }
    }
}
