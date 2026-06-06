using UnityEngine;

namespace GameClient.Gameplay.BaseBuilder
{
    public enum BuildingType
    {
        MainHall,
        Resource,
        Military,
        Decoration
    }

    [System.Serializable]
    public class BuildingLevelStats
    {
        public int Level;
        [Header("Requirements")]
        public int RequiredReputation; // Danh tiếng cần để xây cấp này
        
        [Header("Costs")]
        public int CostGold;
        public int CostWood;
        public int BuildTimeSeconds;
    }

    [CreateAssetMenu(fileName = "NewBuildingData", menuName = "Data/BaseBuilder/BuildingData")]
    public class BuildingData : ScriptableObject
    {
        [Header("Identity")]
        public string BuildingID;
        public string BuildingNameKey;
        public BuildingType Type;

        [Header("Grid Size (Tiles)")]
        [Min(1)] public int SizeX = 2;
        [Min(1)] public int SizeY = 2;

        [Header("Visual")]
        public string PrefabAddress; // Address của Addressable Asset thay vì kéo thả cứng
        public BuildingVisualConfig VisualConfig; // Configuration cho các trạng thái và cấp độ

        [Header("Detail UI (Optional)")]
        public string DetailSubPanelAddress; // Địa chỉ Addressable của Sub-Panel giao diện chi tiết

        [Header("Level Stats & Requirements")]
        public System.Collections.Generic.List<BuildingLevelStats> LevelStats = new System.Collections.Generic.List<BuildingLevelStats>();
    }
}
