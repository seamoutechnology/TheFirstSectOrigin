using UnityEngine;
using UnityEngine.UI;
using GameClient.Managers;
using GameClient.UI.Core;
using GameClient.Core;

namespace GameClient.BaseBuilding.UI
{
    public class SectStatsUI : BaseBehaviour
    {
        [Header("UI References")]
        public Text reputationText;
        public Text alignmentText;
        public Text alignmentTitleText;

        private void OnEnable()
        {
            UpdateUI();
            GameContext.OnServerDataSynced += UpdateUI;
        }

        private void OnDisable()
        {
            GameContext.OnServerDataSynced -= UpdateUI;
        }

        public void UpdateUI()
        {
            if (reputationText != null)
            {
                reputationText.text = $"Danh Tiếng: {GameContext.SectReputation}";
            }

            if (alignmentText != null)
            {
                string alignType = GameContext.SectAlignment >= 0 ? "Chính Phái" : "Tà Phái";
                alignmentText.text = $"Thiện/Ác: {GameContext.SectAlignment} ({alignType})";
            }
        }
    }
}
