using System.Collections.Generic;
using UnityEngine;

namespace GameClient.Gameplay.BaseBuilder
{
    public class BuildingServerConfig
    {
        public string BuildingId;
        public int LevelRequired;
        public int ReputationRequired;
        public int Cost;
    }

    public class ServerConfigManager : GameClient.Singleton<ServerConfigManager>
    {
        private Dictionary<string, BuildingServerConfig> _buildingConfigs = new Dictionary<string, BuildingServerConfig>();

        protected override void Awake()
        {
            base.Awake();
            LoadMockConfigFromServer();
        }

        private void LoadMockConfigFromServer()
        {
            _buildingConfigs.Add("House", new BuildingServerConfig { BuildingId = "House", LevelRequired = 1, ReputationRequired = 0, Cost = 50 });
            _buildingConfigs.Add("Farm", new BuildingServerConfig { BuildingId = "Farm", LevelRequired = 2, ReputationRequired = 100, Cost = 100 });
            _buildingConfigs.Add("RecruitmentCenter", new BuildingServerConfig { BuildingId = "RecruitmentCenter", LevelRequired = 3, ReputationRequired = 500, Cost = 500 });
        }

        public BuildingServerConfig GetBuildingConfig(string buildingId)
        {
            if (_buildingConfigs.TryGetValue(buildingId, out var config))
            {
                return config;
            }
            return null;
        }
    }
}
