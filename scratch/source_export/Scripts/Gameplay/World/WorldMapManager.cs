using UnityEngine;
using GameClient.Managers;

namespace GameClient.Gameplay.World
{
    public class TargetSectInfo
    {
        public string SectName;
        public int Alignment; // Điểm thiện ác của mục tiêu
        public bool IsEvil => Alignment < -50;
        public bool IsGood => Alignment > 50;
        public bool IsNeutral => Alignment >= -50 && Alignment <= 50;
    }

    public class WorldMapManager : GameClient.Singleton<WorldMapManager>
    {
        public bool CanAttack(TargetSectInfo target)
        {
            bool mySectIsEvil = GameContext.IsEvil;
            bool mySectIsGood = GameContext.IsGood;
            bool mySectIsNeutral = GameContext.IsNeutral;

            if (mySectIsNeutral)
            {
                Debug.LogWarning("[WorldMapManager] Bạn là tông môn Trung Lập, không màng thế sự, không thể chủ động tấn công!");
                return false;
            }

            if (target.IsNeutral)
            {
                Debug.LogWarning("[WorldMapManager] Mục tiêu là tông môn Trung Lập, không thể tấn công!");
                return false;
            }

            if (mySectIsEvil)
            {
                return true;
            }

            if (mySectIsGood && target.IsGood)
            {
                Debug.LogWarning("[WorldMapManager] Bạn là Chính Phái, không thể tự ý tấn công một Chính Phái khác!");
                return false;
            }

            return true;
        }
    }
}
