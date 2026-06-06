using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameClient.Managers;
using GameClient.UI.Core;

namespace GameClient.UI
{
    public class MainGameHUDPanel : BaseUIPanel
    {
        [Header("Tài Nguyên (Mẫu)")]
        public TMP_Text txtGold;
        public TMP_Text txtWood;
        public TMP_Text txtStone;
        public TMP_Text txtQi;

        [Header("Nút Tương Tác")]
        public Button btnBuildMenu;
        public Button btnSettings;
        public Button btnWorldMap;

        protected override void OnStart()
        {
            base.OnStart();

            if (btnBuildMenu != null)
            {
                btnBuildMenu.onClick.AddListener(OnBuildMenuClicked);
            }

            if (btnWorldMap != null)
            {
                btnWorldMap.onClick.AddListener(OnWorldMapClicked);
            }
        }

        private void OnBuildMenuClicked()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenPanel("BaseBuildingPanel");
            }
        }

        private void OnWorldMapClicked()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowMessage("World Map", "Tính năng World Map đang được phát triển!");
            }
        }

        public void UpdateResources(int gold, int wood, int stone, int qi)
        {
            if (txtGold != null) txtGold.text = gold.ToString();
            if (txtWood != null) txtWood.text = wood.ToString();
            if (txtStone != null) txtStone.text = stone.ToString();
            if (txtQi != null) txtQi.text = qi.ToString();
        }
    }
}
