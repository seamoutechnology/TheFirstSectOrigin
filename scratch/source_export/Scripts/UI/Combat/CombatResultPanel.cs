using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameClient.UI.Core;
using GameClient.Managers;
using GameClient.Network.Pb;

namespace GameClient.UI.Combat
{
    public class CombatResultPanel : BaseUIPanel
    {
        [Header("UI Headers")]
        [SerializeField] private GameObject victoryHeader;
        [SerializeField] private GameObject defeatHeader;

        [Header("Reward UI Details")]
        [SerializeField] private GameObject rewardContainer;
        [SerializeField] private TMP_Text txtExpReward;
        [SerializeField] private TMP_Text txtLinhThachReward;

        [Header("Defeat Visual Tips")]
        [SerializeField] private GameObject defeatTipsContainer;

        [Header("Buttons")]
        [SerializeField] private Button btnExit;

        private ValidatePvEResultResponse _serverResponse;
        private bool _isVictory;

        protected override void OnInit()
        {
            base.OnInit();
            btnExit.onClick.AddListener(OnExitClicked);
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);

            if (data is ValidatePvEResultResponse resp)
            {
                _serverResponse = resp;
                _isVictory = resp.IsValid; // Validated victory or defeat from server
                
                txtExpReward.text = $"+{resp.RewardExp} EXP";
                txtLinhThachReward.text = $"+{resp.RewardLinhThach} Linh Thạch";
            }
            else if (data is LocalResultData localData)
            {
                _isVictory = localData.IsVictory;
                txtExpReward.text = $"+{localData.RewardExp} EXP";
                txtLinhThachReward.text = $"+{localData.RewardLinhThach} Linh Thạch";
            }
            else
            {
                // Fallback
                _isVictory = false;
                txtExpReward.text = "+0 EXP";
                txtLinhThachReward.text = "+0 Linh Thạch";
            }

            victoryHeader.SetActive(_isVictory);
            defeatHeader.SetActive(!_isVictory);
            rewardContainer.SetActive(_isVictory);
            if (defeatTipsContainer != null)
            {
                defeatTipsContainer.SetActive(!_isVictory);
            }
        }

        private void OnExitClicked()
        {
            Hide();
            if (MapManager.Instance != null)
            {
                _ = MapManager.Instance.LoadMapAsync(MapType.LocalBase);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("LocalBase");
            }
        }

        // Helper struct/class for local testing and offline fallback
        public class LocalResultData
        {
            public bool IsVictory;
            public int RewardExp;
            public int RewardLinhThach;
        }
    }
}
