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
        [SerializeField] private TMP_Text txtRefreshCost;
        [SerializeField] private TMP_Text txtRefreshTimer;

        [Header("Shop Tabs")]
        [SerializeField] private Button btnTabDaily;
        [SerializeField] private Button btnTabGuild;
        [SerializeField] private Button btnTabArena;

        private string currentShopType = "daily";
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

            // Tabs setup
            if (btnTabDaily != null) btnTabDaily.onClick.AddListener(() => SwitchShopTab("daily"));
            if (btnTabGuild != null) btnTabGuild.onClick.AddListener(() => SwitchShopTab("guild"));
            if (btnTabArena != null) btnTabArena.onClick.AddListener(() => SwitchShopTab("arena"));

            // Dynamic lookup fallback if fields are not assigned in Inspector
            FindUiElementsFallback();
        }

        private void FindUiElementsFallback()
        {
            if (btnTabDaily == null)
            {
                var dailyBtnObj = transform.Find("Tabs/Daily") ?? transform.Find("btnTabDaily") ?? transform.Find("DailyTab");
                if (dailyBtnObj != null)
                {
                    btnTabDaily = dailyBtnObj.GetComponent<Button>();
                    btnTabDaily.onClick.AddListener(() => SwitchShopTab("daily"));
                }
            }
            if (btnTabGuild == null)
            {
                var guildBtnObj = transform.Find("Tabs/Guild") ?? transform.Find("btnTabGuild") ?? transform.Find("GuildTab");
                if (guildBtnObj != null)
                {
                    btnTabGuild = guildBtnObj.GetComponent<Button>();
                    btnTabGuild.onClick.AddListener(() => SwitchShopTab("guild"));
                }
            }
            if (btnTabArena == null)
            {
                var arenaBtnObj = transform.Find("Tabs/Arena") ?? transform.Find("btnTabArena") ?? transform.Find("ArenaTab");
                if (arenaBtnObj != null)
                {
                    btnTabArena = arenaBtnObj.GetComponent<Button>();
                    btnTabArena.onClick.AddListener(() => SwitchShopTab("arena"));
                }
            }
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
            if (txtRefreshCost == null)
            {
                var costObj = transform.Find("txtRefreshCost") ?? transform.Find("RefreshCost");
                if (costObj != null) txtRefreshCost = costObj.GetComponent<TMP_Text>();
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            SwitchShopTab("daily");
        }

        private void SwitchShopTab(string shopType)
        {
            currentShopType = shopType;
            UpdateTabVisuals();
            _ = LoadShopDataFromServer();
        }

        private void UpdateTabVisuals()
        {
            if (btnTabDaily != null) btnTabDaily.image.color = currentShopType == "daily" ? Color.green : Color.white;
            if (btnTabGuild != null) btnTabGuild.image.color = currentShopType == "guild" ? Color.green : Color.white;
            if (btnTabArena != null) btnTabArena.image.color = currentShopType == "arena" ? Color.green : Color.white;
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
                    
                    UpdateRefreshCostLabel();
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

                string itemName = LocalizationManager.Instance.GetText("item", item.ItemCode) ?? item.ItemCode;
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

                newCard.Setup(
                    item.Id,
                    item.ItemCode,
                    itemName,
                    priceAmount,
                    currency,
                    item.DiscountPct,
                    item.IsBought,
                    iconSprite,
                    OnBuyItemClicked
                );

                spawnedItems.Add(newCard);
            }

            UpdateRefreshCostLabel();
        }

        private async Task LoadIconAsync(string key, UI_ShopItem card)
        {
            try
            {
                Sprite s = await ResourceManager.Instance.LoadAssetAsync<Sprite>(key);
                if (s != null && card != null)
                {
                    var img = card.GetComponentInChildren<Image>();
                    if (img != null) img.sprite = s;
                }
            }
            catch
            {
                // Fallback
            }
        }

        private void UpdateRefreshCostLabel()
        {
            if (txtRefreshCost != null)
            {
                bool hasTicket = HasRefreshTicket();
                txtRefreshCost.text = hasTicket ? "Refresh: 1 ticket" : "Refresh: 50 Diamond";
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
            string confirmMsg = $"Bạn có muốn tiêu hao {itemCard.PriceAmount} {itemCard.CurrencyType} để mua {itemCard.ItemCode}?";
            
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
                    txtRefreshTimer.text = string.Format("F5 sau: {0:D2}:{1:D2}:{2:D2}", diff.Hours, diff.Minutes, diff.Seconds);
                }
                else
                {
                    txtRefreshTimer.text = "Có thể làm mới!";
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
