using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
namespace GameClient.Gameplay.BaseBuilder
{
    [Serializable]
    public class TileMapping
    {
        public TileBase tile;
        public string entityID; // e.g. "main_hall", "tree_01"
        public int defaultLevel = 1; // Dùng cho Building
        public BuildingState defaultState = BuildingState.Normal; // Dùng cho Building
    }

    [CreateAssetMenu(fileName = "TileMapping", menuName = "Tools/TileToIDMapping")]
    public class TileToIDMapping : ScriptableObject
    {
        [Header("Mapping config for Exporter")]
        public List<TileMapping> buildingMappings = new List<TileMapping>();
        public List<TileMapping> itemMappings = new List<TileMapping>();
        public List<TileMapping> groundMappings = new List<TileMapping>();
        
        public List<TileBase> unbuildableTerrainTiles = new List<TileBase>();

        public string GetGroundID(TileBase tile)
        {
            var match = groundMappings.Find(x => x.tile == tile);
            return match != null ? match.entityID : string.Empty;
        }

        public string GetBuildingID(TileBase tile)
        {
            var match = buildingMappings.Find(x => x.tile == tile);
            return match != null ? match.entityID : string.Empty;
        }

        public BuildingState GetBuildingState(TileBase tile)
        {
            var match = buildingMappings.Find(x => x.tile == tile);
            return match != null ? match.defaultState : BuildingState.Normal;
        }

        public int GetBuildingLevel(TileBase tile)
        {
            var match = buildingMappings.Find(x => x.tile == tile);
            return match != null ? match.defaultLevel : 1;
        }

        public string GetItemID(TileBase tile)
        {
            var match = itemMappings.Find(x => x.tile == tile);
            return match != null ? match.entityID : string.Empty;
        }

        public bool IsUnbuildable(TileBase tile)
        {
            return unbuildableTerrainTiles.Contains(tile);
        }

        public TileBase GetGroundTile(string entityID)
        {
            var match = groundMappings.Find(x => x.entityID == entityID);
            return match != null ? match.tile : null;
        }

        public TileBase GetItemTile(string entityID)
        {
            var match = itemMappings.Find(x => x.entityID == entityID);
            return match != null ? match.tile : null;
        }
    }
}
