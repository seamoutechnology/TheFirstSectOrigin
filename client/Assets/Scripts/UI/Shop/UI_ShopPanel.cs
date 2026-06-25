using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using GameClient.UI.Core;
using GameClient.Managers;
using GameClient.Core;
using GameClient.Network.Api;
using GameClient.Network.Pb;

namespace GameClient.UI
{
    public class UI_ShopPanel : BaseUIPanel
    {
        [Header("UI References")]
        [SerializeField] private UI_ShopItem itemPrefab;
        [SerializeField] private Transform contentContainer;
        [SerializeField] private Button btnClose;
        [SerializeField] private Button btnRefresh;
        [SerializeField] private TMP_Text txtRefreshTimer;

        [Header("Shop Settings")]
        [SerializeField] private string currentShopType = "daily";

        private List<UI_ShopItem> spawnedItems = new List<UI_ShopItem>();
        private System.DateTime nextRefreshTime;
        private bool isTimerActive = false;
        private Inventory cachedInventory;

        protected override void OnStart()
        {
            base.OnStart();

            if (btnClose != null)
            {
                btnClose.onClick.AddListener(ClosePanel);
            }

            if (btnRefresh != null)
            {
                btnRefresh.onClick.AddListener(OnRefreshClicked);
            }

            if (itemPrefab != null)
            {
                itemPrefab.gameObject.SetActive(false); // Hide the template
            }

            // Dynamic lookup fallback if fields are not assigned in Inspector
            FindUiElementsFallback();
        }

        private void FindUiElementsFallback()
        {
            if (btnRefresh == null)
            {
                var refreshBtnObj = transform.Find("btnRefresh") ?? transform.Find("RefreshButton");
                if (refreshBtnObj != null)
                {
                    btnRefresh = refreshBtnObj.GetComponent<Button>();
                    btnRefresh.onClick.AddListener(OnRefreshClicked);
                }
            }
            if (txtRefreshTimer == null)
            {
                var timerObj = transform.Find("txtRefreshTimer") ?? transform.Find("RefreshTimer");
                if (timerObj != null) txtRefreshTimer = timerObj.GetComponent<TMP_Text>();
            }

        }

        protected override void OnShow()
        {
            base.OnShow();
            _ = LoadShopDataFromServer();
        }

        private async Task LoadShopDataFromServer()
        {
            ClearShop();
            
            try
            {
                var shopTask = SectBuildingApi.GetShopAsync(currentShopType);
                var inventoryTask = SectBuildingApi.GetInventoryAsync();
                
                await Task.WhenAll(shopTask, inventoryTask);
                
                var response = shopTask.Result;
                cachedInventory = inventoryTask.Result;
                
                if (response != null && response.Code == 0)
                {
                    RenderShopItems(response.Items);
                    
                    long epoch = response.NextRefreshAt;
                    nextRefreshTime = System.DateTimeOffset.FromUnixTimeSeconds(epoch).LocalDateTime;
                    isTimerActive = true;
                }
                else
                {
                    string errMsg = response != null ? response.MessageId : "Lỗi tải dữ liệu cửa hiệu";
                    UIManager.Instance.ShowMessage("Lỗi", errMsg);
                }
            }
            catch (System.Exception ex)
            {
                UIManager.Instance.ShowMessage("Lỗi", $"Không thể kết nối đến máy chủ: {ex.Message}");
            }
        }

        private void RenderShopItems(IEnumerable<ShopItemInstance> items)
        {
            foreach (var item in items)
            {
                UI_ShopItem newCard = Instantiate(itemPrefab, contentContainer);
                newCard.gameObject.SetActive(true);

                string itemName = LocalizationManager.Instance.GetText("Item_Equipment", item.ItemCode);
                if (string.IsNullOrEmpty(itemName) || itemName.StartsWith("["))
                {
                    itemName = item.ItemCode;
                }
                string iconKey = $"{item.ItemCode}_icon";

                Sprite iconSprite = null;
                _ = LoadIconAsync(iconKey, newCard);

                string currency = "Gold";
                int priceAmount = 0;
                if (item.FinalPrice != null && item.FinalPrice.Count > 0)
                {
                    currency = item.FinalPrice[0].ItemCode;
                    priceAmount = item.FinalPrice[0].Amount;
                }

                string currencyName = LocalizationManager.Instance.GetText("Item_Equipment", currency);
                if (string.IsNullOrEmpty(currencyName) || currencyName.StartsWith("["))
                {
                    currencyName = currency;
                }

                newCard.Setup(
                    item.Id,
                    item.ItemCode,
                    itemName,
                    priceAmount,
                    currencyName,
                    item.DiscountPct,
                    item.IsBought,
                    iconSprite,
                    OnBuyItemClicked
                );

                spawnedItems.Add(newCard);
            }
        }

        private async Task LoadIconAsync(string key, UI_ShopItem card)
        {
            try
            {
                Sprite s = await ResourceManager.Instance.LoadAssetAsync<Sprite>(key);
                if (s != null && card != null)
                {
                    card.SetIcon(s);
                }
            }
            catch
            {
                // Fallback
            }
        }



        private bool HasRefreshTicket()
        {
            if (cachedInventory == null || cachedInventory.Items == null) return false;
            
            foreach (var it in cachedInventory.Items)
            {
                if (it.ItemCode == "shop_reset_ticket" && it.Quantity > 0)
                {
                    return true;
                }
            }
            return false;
        }

        private void OnBuyItemClicked(UI_ShopItem itemCard)
        {
            string localizedItemName = LocalizationManager.Instance.GetText("Item_Equipment", itemCard.ItemCode);
            if (string.IsNullOrEmpty(localizedItemName) || localizedItemName.StartsWith("["))
            {
                localizedItemName = itemCard.ItemCode;
            }
            string confirmMsg = $"Bạn có muốn tiêu hao {itemCard.PriceAmount} {itemCard.CurrencyType} để mua {localizedItemName}?";
            
            UIManager.Instance.ShowConfirmDialog(
                "Mua Vật Phẩm",
                confirmMsg,
                "",
                "Mua",
                "Hủy",
                async () =>
                {
                    string failedTitle = "Mua Thất Bại";
                    string successTitle = "Thành Công";

                    try
                    {
                        var response = await SectBuildingApi.BuyShopItemAsync(itemCard.InstanceId, 1);
                        if (response != null && response.Code == 0)
                        {
                            UIManager.Instance.ShowMessage(successTitle, "Mua thành công!");
                            if (GameManager.Instance != null)
                            {
                                var profileRes = await GameClient.Network.Api.PlayerApi.GetPlayerProfileAsync();
                                if (profileRes != null && profileRes.Base != null && profileRes.Base.Code == 0)
                                {
                                    GameManager.Instance.SetPlayer(profileRes.Profile);
                                }
                            }
                            _ = LoadShopDataFromServer();
                        }
                        else
                        {
                            string errMsg = response != null ? response.MessageId : "Lỗi hệ thống";
                            UIManager.Instance.ShowMessage(failedTitle, errMsg);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        UIManager.Instance.ShowMessage(failedTitle, $"Lỗi: {ex.Message}");
                    }
                }
            );
        }

        private void OnRefreshClicked()
        {
            bool hasTicket = HasRefreshTicket();
            string confirmMsg = hasTicket 
                ? "Bạn có muốn tiêu hao 1 Vé Làm Mới để làm mới Cửa Hiệu?" 
                : "Bạn có muốn tiêu hao 50 Kim Cương để làm mới Cửa Hiệu?";

            UIManager.Instance.ShowConfirmDialog(
                "Làm Mới Cửa Hiệu",
                confirmMsg,
                "",
                "Đồng Ý",
                "Hủy",
                async () =>
                {
                    try
                    {
                        var response = await SectBuildingApi.RefreshShopAsync(currentShopType);
                        if (response != null && response.Code == 0)
                        {
                            UIManager.Instance.ShowMessage("Thành Công", "Làm mới cửa hiệu thành công!");
                            if (GameManager.Instance != null)
                            {
                                var profileRes = await GameClient.Network.Api.PlayerApi.GetPlayerProfileAsync();
                                if (profileRes != null && profileRes.Base != null && profileRes.Base.Code == 0)
                                {
                                    GameManager.Instance.SetPlayer(profileRes.Profile);
                                }
                            }
                            RenderShopItems(response.Items);
                            long epoch = response.NextRefreshAt;
                            nextRefreshTime = System.DateTimeOffset.FromUnixTimeSeconds(epoch).LocalDateTime;
                        }
                        else
                        {
                            string errMsg = response != null ? response.MessageId : "Không đủ tài nguyên để làm mới";
                            UIManager.Instance.ShowMessage("Lỗi", errMsg);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        UIManager.Instance.ShowMessage("Lỗi", $"Lỗi: {ex.Message}");
                    }
                }
            );
        }

        private void Update()
        {
            if (isTimerActive && txtRefreshTimer != null)
            {
                var diff = nextRefreshTime - System.DateTime.Now;
                if (diff.TotalSeconds > 0)
                {
                    txtRefreshTimer.text = string.Format("{0:D2}:{1:D2}:{2:D2}", diff.Hours, diff.Minutes, diff.Seconds);
                }
                else
                {
                    txtRefreshTimer.text = "00:00:00";
                    isTimerActive = false;
                    _ = LoadShopDataFromServer(); // Auto refresh
                }
            }
        }

        private void ClearShop()
        {
            foreach (var item in spawnedItems)
            {
                if (item != null) Destroy(item.gameObject);
            }
            spawnedItems.Clear();
        }

        private void ClosePanel()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ClosePanel("UI_ShopPanel");
            }
        }
    }
}
