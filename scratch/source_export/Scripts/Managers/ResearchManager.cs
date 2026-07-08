using UnityEngine;
using GameClient.Core;

namespace GameClient.Managers
{
    public class ResearchManager : Singleton<ResearchManager>
    {
        protected override void Awake()
        {
            base.Awake();
        }

        public bool IsBranchUnlocked(int branchIndex)
        {
            if (ServerConfigManager.Instance != null && ServerConfigManager.Instance.CurrentConfig != null)
            {
                if (branchIndex < ServerConfigManager.Instance.CurrentConfig.max_free_research)
                {
                    return true; // Được miễn phí theo luật Server
                }
            }
            else
            {
                Debug.LogWarning("[ResearchManager] Chưa load được Server Config!");
            }

            string requiredPackageId = $"unlock_research_branch_{branchIndex}";

            if (PlayerProfileManager.Instance != null && PlayerProfileManager.Instance.HasActivePackage(requiredPackageId))
            {
                return true; // Mở khóa vì đã Mua Gói (Và còn hạn)
            }

            return false;
        }

        public void TestUnlockStatus()
        {
            Debug.Log($"Trạng thái Nhánh 1 (Index 0): {IsBranchUnlocked(0)}");
            Debug.Log($"Trạng thái Nhánh 2 (Index 1): {IsBranchUnlocked(1)}");
        }
    }
}
