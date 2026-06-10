using UnityEngine;
using System.Collections.Generic;

namespace GameClient.Gameplay.Heroes
{
    [System.Serializable]
    public class TraitEffect
    {
        public string effectCode;
        public float value;
    }

    [CreateAssetMenu(fileName = "Trait_", menuName = "Gameplay/Trait Data")]
    public class TraitData : ScriptableObject
    {
        public string traitCode;
        public string nameKey;
        public string descriptionKey;
        public int spawnWeight = 100; // Trọng số xuất hiện trên server
        public List<TraitEffect> effects = new List<TraitEffect>();
    }
}
