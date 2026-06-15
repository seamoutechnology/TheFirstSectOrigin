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

    public enum BuildingCategory
    {
        Production, // Sản xuất
        Facility,   // Cơ sở
        Shop,       // Cửa Hiệu
        Scenery     // Phong Cảnh
    }

    [System.Serializable]
    public class ItemRequirement
    {
        public string ItemCode;
        public int Quantity;
    }

    [System.Serializable]
    public class ReputationLimit
    {
        public int RequiredReputation; // Cột mốc danh tiếng/uy danh
        public int MaxAllowed;         // Số lượng tối đa được phép xây ở cột mốc này
    }

    [System.Serializable]
    public class BuildingLevelStats
    {
        public int Level;
        [Header("Requirements")]
        public int RequiredReputation; // Danh tiếng cần để xây cấp này
        
        [Header("Costs")]
        public System.Collections.Generic.List<ItemRequirement> CostItems;
        public int BuildTimeSeconds;
    }

    [CreateAssetMenu(fileName = "NewBuildingData", menuName = "Data/BaseBuilder/BuildingData")]
    public class BuildingData : ScriptableObject
    {
        [Header("Identity")]
        public string BuildingID;
        public string BuildingNameKey;
        public string BuildingDescKey;
        public BuildingType Type;
        public BuildingCategory Category = BuildingCategory.Facility;

        [Header("Reputation Limits")]
        public System.Collections.Generic.List<ReputationLimit> ReputationLimits = new System.Collections.Generic.List<ReputationLimit>();

        public int GetMaxLimit(int currentReputation)
        {
            if (Type == BuildingType.MainHall) return 1; // Nhà Chính chỉ được phép xây tối đa 1

            if (ReputationLimits == null || ReputationLimits.Count == 0) return -1; // -1 nghĩa là vô hạn

            int maxAllowed = 0;
            var sortedLimits = new System.Collections.Generic.List<ReputationLimit>(ReputationLimits);
            sortedLimits.Sort((a, b) => a.RequiredReputation.CompareTo(b.RequiredReputation));

            foreach (var limit in sortedLimits)
            {
                if (currentReputation >= limit.RequiredReputation)
                {
                    maxAllowed = limit.MaxAllowed;
                }
                else
                {
                    break;
                }
            }

            return maxAllowed;
        }

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
