using UnityEngine;

namespace GameClient.Gameplay.Heroes
{
    [CreateAssetMenu(fileName = "NewHeroConfig", menuName = "GameData/Hero Config")]
    public class HeroConfig : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("ID của tướng, phải khớp với server")]
        public long heroId;
        
        [Tooltip("Tên hiển thị của tướng")]
        public string heroName;
        
        [Tooltip("Mô tả tiểu sử")]
        [TextArea(3, 5)]
        public string description;

        [Header("Tuổi Thọ & Trung Thành")]
        [Tooltip("Thọ nguyên tối đa ban đầu của tướng (tính theo năm game)")]
        public int MaxLifespan = 100;
        
        [Tooltip("Độ trung thành cơ bản khi mới thu nhận (0 - 100)")]
        public int BaseLoyalty = 80;

        [Header("Assets")]
        [Tooltip("Addressable Key của Prefab (Model 3D hoặc UI Prefab)")]
        public string prefabAddress;
        
        [Tooltip("Addressable Key của Avatar (Sprite/Texture)")]
        public string iconAddress;

        [Header("Stats")]
        public string rarity = "R"; // UR, SSR, SR, R
        public float basePower = 100f;
        
        [Header("Server Synchronized Fields")]
        public string code;
        public string element = "FIRE";
        public string role = "WARRIOR";
        public int baseHp = 100;
        public int baseAtk = 10;
        public int baseDef = 5;
        public int baseSpeed = 10;
        public int gachaWeight = 100;
        public bool isActive = true;
    }
}
