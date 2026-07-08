using UnityEngine;

namespace GameClient.Gameplay.BaseBuilder
{
    public enum ResourceType
    {
        SpiritHerb,   
        SpiritStone,  
        Wood         
    }

    [CreateAssetMenu(fileName = "NewProductionBuildingData", menuName = "Data/BaseBuilder/ProductionBuildingData")]
    public class ProductionBuildingData : BuildingData
    {
        [Header("Production Settings")]
        [Tooltip("Mã vật phẩm sẽ sản xuất ra (ví dụ: 00001 là Vàng)")]
        public string ProducedItemCode = "00001";
        public float ProductionRatePerSecond = 1.0f; 
        public int MaxCapacity = 100; 
    }
}
