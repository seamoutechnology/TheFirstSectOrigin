using UnityEngine;
using GameClient.Gameplay.BaseBuilder;

namespace GameClient.UI
{
    public abstract class BuildingSubPanel : MonoBehaviour
    {
        public abstract void Setup(BuildingInstance building);
    }
}
