using UnityEngine;
using UnityEngine.UI;
using GameClient.Network;
using GameClient.Network.Api;
using GameClient.UI.Core;

namespace GameClient.UI
{
    public class BaseBuildingPanel : BaseUIPanel
    {
        [Header("UI References")]
        [SerializeField] private Transform buildingListContainer;
        [SerializeField] private GameObject buildingItemPrefab;
        [SerializeField] private Button closeButton;

        protected override void OnStart()
        {
            base.OnStart();
            closeButton.onClick.AddListener(Hide); // Gọi hàm Hide() của BaseUIPanel
        }

        protected override void OnShow()
        {
            base.OnShow();
            RefreshUI();
        }

        private void RefreshUI()
        {
            foreach (Transform child in buildingListContainer)
                Destroy(child.gameObject);

            var buildings = GameManager.Instance.PlayerBuildings;
            foreach (var b in buildings)
            {
                var go = Instantiate(buildingItemPrefab, buildingListContainer);
                var texts = go.GetComponentsInChildren<Text>();
                texts[0].text = b.Name;
                texts[1].text = $"Lv.{b.Level}/{b.MaxLevel}";
                
                if (b.UpgradeEndAt > 0)
                {
                    var endAt = System.DateTimeOffset.FromUnixTimeSeconds(b.UpgradeEndAt).ToLocalTime();
                    texts[2].text = $"Đang nâng cấp, xong lúc: {endAt:HH:mm:ss}";
                }
                else
                {
                    texts[2].text = $"Đang chờ (Vàng chờ: {b.PendingGold})";
                }

                var buttons = go.GetComponentsInChildren<Button>();
                var collectBtn = buttons[0];
                var upgradeBtn = buttons[1];

                collectBtn.interactable = b.PendingGold > 0 && b.UpgradeEndAt == 0;
                collectBtn.onClick.AddListener(() => OnCollectClicked(b.BuildingCode));

                upgradeBtn.interactable = b.UpgradeEndAt == 0 && b.Level < b.MaxLevel;
                upgradeBtn.onClick.AddListener(() => OnUpgradeClicked(b.BuildingCode));
            }
        }

        private async void OnCollectClicked(string code)
        {
            try
            {
                var resp = await SectBuildingApi.CollectResourcesAsync(code);
                if (resp.Base.Code == 0)
                {
                    Log($"Thu thập thành công: {resp.GoldGained} vàng!");
                    GameManager.Instance.SetPlayer(resp.Player);
                    
                    var baseResp = await SectBuildingApi.GetBaseAsync();
                    if (baseResp.Base.Code == 0)
                    {
                        GameManager.Instance.SetBuildings(baseResp.Buildings);
                        RefreshUI();
                    }
                }
                else LogError($"Lỗi thu thập: {resp.Base.Message}");
            }
            catch (System.Exception ex) { LogError(ex.ToString()); }
        }

        private async void OnUpgradeClicked(string code)
        {
            try
            {
                var resp = await SectBuildingApi.UpgradeBuildingAsync(code);
                if (resp.Base.Code == 0)
                {
                    Log("Đã bắt đầu nâng cấp!");
                    var baseResp = await SectBuildingApi.GetBaseAsync();
                    if (baseResp.Base.Code == 0)
                    {
                        GameManager.Instance.SetBuildings(baseResp.Buildings);
                        RefreshUI();
                    }
                }
                else LogError($"Lỗi nâng cấp: {resp.Base.Message}");
            }
            catch (System.Exception ex) { LogError(ex.ToString()); }
        }
    }
}
