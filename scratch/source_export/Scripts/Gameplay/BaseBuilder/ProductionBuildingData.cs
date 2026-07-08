using UnityEngine;

namespace GameClient.Gameplay.BaseBuilder
{
    public enum ResourceType
    {
        SpiritHerb,   // Linh Tháº£o
        SpiritStone,  // Linh Tháº¡ch
        Wood          // Gá»—
    }

    [CreateAssetMenu(fileName = "NewProductionBuildingData", menuName = "Data/BaseBuilder/ProductionBuildingData")]
    public class ProductionBuildingData : BuildingData
    {
        [Header("Production Settings")]
        public ResourceType ProducedResource;
        public float ProductionRatePerSecond = 1.0f; // Sáº£n lÆ°á»£ng má»—i giÃ¢y
        public int MaxCapacity = 100; // Sá»©c chá»©a tá»‘i Ä‘a, Ä‘áº§y thÃ¬ sáº½ á»Ÿ tráº¡ng thÃ¡i ReadyToHarvest
    }
}
