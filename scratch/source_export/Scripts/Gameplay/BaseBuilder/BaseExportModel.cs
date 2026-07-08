using System;
using System.Collections.Generic;

namespace GameClient.Gameplay.BaseBuilder
{
    public enum BuildingState
    {
        Normal = 0,
        Ghost = 1,
        Building = 2,
        Upgrading = 3,
        Broken = 4,
        Locked = 5,
        Producing = 6, // Ä ang trá»“ng / sáº£n xuáº¥t
        ReadyToHarvest = 7 // Ä Ã£ chÃ­n / Sáºµn sÃ ng thu hoáº¡ch
    }

    [Serializable]
    public class ExportedBuilding
    {
        public long instance_id; // Database Primary Key ID
        public string id;  // BuildingID
        public int x;      // Tọa độ X trên Grid
        public int y;      // Tọa độ Y trên Grid
        public bool flipX; // Hướng (true: lật ngược)
        public int level = 1; // Cấp độ công trình
        public BuildingState state = BuildingState.Normal; // Trạng thái: Normal, Ghost, Building, Upgrading, Broken, Locked
        public float currentHP = -1; // Máu hiện tại (-1 = max máu)
    }

    [Serializable]
    public class ExportedItem
    {
        public string id;  // ItemID
        public int x;
        public int y;
        public bool flipX;
        public int quantity = 1; // Số lượng đồ rơi, rương
    }

    [Serializable]
    public class ExportedGroundLayer
    {
        public string layerName;
        public string[] tiles;
    }

    [Serializable]
    public class BaseExportModel
    {
        public int gridWidth;
        public int gridHeight;
        
        public int[] fogData;
        
        public List<ExportedItem> items = new List<ExportedItem>();

        public List<ExportedBuilding> buildings = new List<ExportedBuilding>();

        public int[] terrainData;

        public string[] groundTiles; // Flat array of tiles (e.g. from DefaultMap.json or legacy exports)

        public List<ExportedGroundLayer> groundLayers = new List<ExportedGroundLayer>();
    }
}
