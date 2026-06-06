using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameClient.Gameplay.BaseBuilder
{
    [Serializable]
    public class BuildingLevelVisual
    {
        public int level = 1;
        
        [Header("Sprites By State")]
        public Sprite normalSprite;
        public Sprite ghostSprite; 
        public Sprite buildingSprite; 
        public Sprite upgradingSprite; 
        public Sprite brokenSprite; 
        public Sprite lockedSprite; 
        
        [Header("Production States (Farms/Miners)")]
        public Sprite producingSprite;
        public Sprite readyToHarvestSprite;
    }

    [CreateAssetMenu(fileName = "New Building Visual Config", menuName = "Tools/Building Visual Config")]
    public class BuildingVisualConfig : ScriptableObject
    {
        [Header("Building ID matched with Server")]
        public string buildingID; // VD: "main_hall"

        [Header("VFX Prefabs (Optional)")]
        [Tooltip("Hiá»‡u á»©ng bá»¥i khá»‘i lÃºc Ä‘ang xÃ¢y hoáº·c nÃ¢ng cáº¥p")]
        public GameObject constructionVFXPrefab;
        [Tooltip("Hiá»‡u á»©ng lÃºc nhÃ  Ä‘ang hoáº¡t Ä‘á»™ng/sáº£n xuáº¥t (vÃ­ dá»¥ khÃ³i bÃªp, lá»  rÃ¨n)")]
        public GameObject workingVFXPrefab;

        [Header("Level Visuals")]
        public List<BuildingLevelVisual> levelVisuals = new List<BuildingLevelVisual>();

        public BuildingLevelVisual GetVisualsForLevel(int level)
        {
            var match = levelVisuals.Find(x => x.level == level);
            if (match == null && levelVisuals.Count > 0)
            {
                return levelVisuals[0];
            }
            return match;
        }
    }
}
