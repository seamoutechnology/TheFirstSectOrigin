using UnityEngine;
using System.Collections.Generic;

namespace GameClient.Gameplay.World
{
    [CreateAssetMenu(fileName = "Stage_", menuName = "PVE/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Header("Basic Info")]
        public string stageId;
        public string stageName;
        public string description;
        public int recommendPower;
        public int staminaCost = 5;
        [Tooltip("Stage ID yêu cầu phải hoàn thành để mở khóa ải này. Để trống nếu là ải đầu.")]
        public string requiredStageId;

        [Header("Map & Scene Settings")]
        public string combatSceneName = "Dungeon"; // Scene loaded when fighting
        public Sprite mapIcon;                     // Map selection icon

        [Header("Enemies Configuration")]
        public List<MonsterConfig> enemiesConfig;

        [Header("Rewards Config")]
        public List<RewardConfig> rewards;
    }

    [System.Serializable]
    public class MonsterConfig
    {
        public string monsterId;
        public string name;
        public int level;
        public int maxHP;
        public int attack;
        public int defense;
        public int speed;
        public bool isBoss;
        public string prefabAddress; // Serializable Addressable Key for Visual Prefab
        [System.NonSerialized] public GameObject prefabVisual; // Runtime visual prefab reference
    }

    [System.Serializable]
    public class RewardConfig
    {
        public string itemId;
        public int amount;
    }
}
