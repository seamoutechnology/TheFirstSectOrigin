using UnityEngine;
using UnityEngine.UI;
using GameClient.Network;
using GameClient.Network.Api;
using GameClient.UI.Core;

namespace GameClient.UI
{
    public class GachaPanel : BaseUIPanel
    {
        [Header("UI References")]
        [SerializeField] private Text bannerNameText;
        [SerializeField] private Text costText;
        [SerializeField] private Button pull1Button;
        [SerializeField] private Button pull10Button;
        [SerializeField] private Button closeButton;
        [SerializeField] private Text statusText;

        private int _currentBannerId = -1;

        protected override void OnStart()
        {
            base.OnStart();
            closeButton.onClick.AddListener(Hide);
            pull1Button.onClick.AddListener(() => DoPull(1));
            pull10Button.onClick.AddListener(() => DoPull(10));
        }

        protected override async void OnShow()
        {
            base.OnShow();
            statusText.text = "Đang tải Banner...";
            SetButtonsActive(false);

            try
            {
                var resp = await GachaApi.GetGachaBannersAsync();
                if (resp.Base.Code == 0 && resp.Banners.Count > 0)
                {
                    var banner = resp.Banners[0];
                    _currentBannerId = banner.BannerId;
                    bannerNameText.text = banner.Name;
                    
                    if (!string.IsNullOrEmpty(banner.CostItem))
                    {
                        costText.text = $"1x [Vật Phẩm {banner.CostItem}] / Lượt";
                    }
                    else if (banner.CostGold > 0)
                    {
                        costText.text = $"{banner.CostGold} Vàng / Lượt";
                    }
                    else
                    {
                        costText.text = $"{banner.CostDiamond} Kim Cương / Lượt";
                    }

                    statusText.text = banner.Description;
                    SetButtonsActive(true);
                }
                else statusText.text = "Không có Banner nào.";
            }
            catch (System.Exception ex)
            {
                statusText.text = "Lỗi khi tải Banner.";
                LogError(ex.ToString());
            }
        }

        private async void DoPull(int count)
        {
            if (_currentBannerId == -1) return;

            statusText.text = $"Đang triệu hồi {count} lần...";
            SetButtonsActive(false);

            try
            {
                var resp = await GachaApi.DoGachaAsync(_currentBannerId, count);
                if (resp.Base.Code == 0)
                {
                    GameManager.Instance.SetPlayer(resp.PlayerAfter);

                    string resultStr = "Bạn nhận được:\n";
                    foreach (var h in resp.Heroes)
                    {
                        resultStr += $"[{h.Rarity}] {h.Name}\n";
                        GameManager.Instance.PlayerHeroes.Add(h);
                    }
                    
                    GameManager.Instance.SetHeroes(GameManager.Instance.PlayerHeroes);
                    statusText.text = resultStr;
                }
                else statusText.text = $"Lỗi: {resp.Base.Message}";
            }
            catch (System.Exception ex)
            {
                statusText.text = "Lỗi kết nối Server.";
                LogError(ex.ToString());
            }
            finally { SetButtonsActive(true); }
        }

        private void SetButtonsActive(bool active)
        {
            pull1Button.interactable = active;
            pull10Button.interactable = active;
        }
    }
}
