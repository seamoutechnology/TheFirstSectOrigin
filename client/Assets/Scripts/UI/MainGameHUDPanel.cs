using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using GameClient.Managers;
using GameClient.UI.Core;
using GameClient.Core;
using GameClient.Network.Pb;

namespace GameClient.UI
{
    public class MainGameHUDPanel : BaseUIPanel
    {
        [Header("Cấu hình Render Động")]
        [Tooltip("Container (ví dụ: Horizontal Layout Group) chứa các Item tài nguyên sẽ hiển thị")]
        public Transform currencyContainer;

        [Tooltip("Prefab của ô tài nguyên HUDCurrencyItem")]
        public HUDCurrencyItem currencyItemPrefab;

        [Header("Nút Tương Tác")]
        public Button btnBuildMenu;
        public Button btnInventory;
        public Button btnDisciples;
        public Button btnWorldMap;
        public Button btnSettings;
        public Button btnLeaderboard;
        public Button btnShop;

        // Quản lý các ô tài nguyên đang được hiển thị
        private List<HUDCurrencyItem> activeHUDItems = new List<HUDCurrencyItem>();
        
        // Lưu trữ các giá trị tài nguyên hiện tại
        private Dictionary<string, int> currentResourceAmounts = new Dictionary<string, int>();

        // Danh sách item key cần hiển thị ở thời điểm hiện tại
        private string[] currentActiveKeys = new string[0];

        protected override void OnStart()
        {
            base.OnStart();

            // Tự động gán các nút bị thiếu từ Inspector (tránh lỗi null link trong prefab)
            if (btnBuildMenu == null) btnBuildMenu = FindButtonByName("BuildMenu") ?? FindButtonByName("Build Menu");
            if (btnInventory == null) btnInventory = FindButtonByName("Inventory");
            if (btnDisciples == null) btnDisciples = FindButtonByName("Princlple") ?? FindButtonByName("Princple") ?? FindButtonByName("Disciples") ?? FindButtonByName("Hero");
            if (btnWorldMap == null) btnWorldMap = FindButtonByName("World Map") ?? FindButtonByName("WorldMap");
            if (btnSettings == null) btnSettings = FindButtonByName("Settings") ?? FindButtonByName("Setting");
            if (btnLeaderboard == null) btnLeaderboard = FindButtonByName("Leaderboard") ?? FindButtonByName("Rank") ?? FindButtonByName("XepHang");
            if (btnShop == null) btnShop = FindButtonByName("Shop") ?? FindButtonByName("Store") ?? FindButtonByName("CuaHang");

            if (btnBuildMenu != null)
            {
                btnBuildMenu.onClick.AddListener(OnBuildMenuClicked);
            }

            if (btnInventory != null)
            {
                btnInventory.onClick.AddListener(OnInventoryClicked);
            }

            if (btnDisciples != null)
            {
                btnDisciples.onClick.AddListener(OnDisciplesClicked);
            }

            if (btnWorldMap != null)
            {
                btnWorldMap.onClick.AddListener(OnWorldMapClicked);
            }

            if (btnSettings != null)
            {
                btnSettings.onClick.AddListener(OnSettingsClicked);
            }

            if (btnLeaderboard != null)
            {
                btnLeaderboard.onClick.AddListener(OnLeaderboardClicked);
            }

            if (btnShop != null)
            {
                btnShop.onClick.AddListener(OnShopClicked);
            }

            // Ẩn prefab gốc nếu nó đang nằm trong Scene
            if (currencyItemPrefab != null && currencyItemPrefab.gameObject.activeSelf)
            {
                currencyItemPrefab.gameObject.SetActive(false);
            }

            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

            // Đăng ký sự kiện cập nhật profile để tự động refresh tài nguyên lập tức
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerUpdated -= HandlePlayerUpdated;
                GameManager.Instance.OnPlayerUpdated += HandlePlayerUpdated;
            }
        }

        private void HandlePlayerUpdated(PlayerProfile profile)
        {
            RefreshResources();
        }

        protected override void OnShow()
        {
            base.OnShow();
            
            SetupHUD(new string[] { "stamina", "00001", "00000" });
        }

        /// <summary>
        /// Đầu vào (Input): Nhận mảng các item key (ItemCode) cần hiển thị.
        /// Sinh ra các Prefab tương ứng và fetch dữ liệu tài nguyên/tiền tệ.
        /// </summary>
        public void SetupHUD(string[] activeCurrencyCodes)
        {
            if (activeCurrencyCodes == null) return;
            currentActiveKeys = activeCurrencyCodes;

            // 1. Xóa các Item cũ đang hiển thị
            ClearActiveItems();

            if (currencyContainer == null || currencyItemPrefab == null)
            {
                Debug.LogWarning("[MainGameHUDPanel] Chưa cấu hình Container hoặc Prefab mẫu!");
                return;
            }

            // 2. Instantiate các item theo danh sách key
            foreach (var code in activeCurrencyCodes)
            {
                if (string.IsNullOrEmpty(code)) continue;

                HUDCurrencyItem newItem = Instantiate(currencyItemPrefab, currencyContainer);
                newItem.gameObject.SetActive(true);
                newItem.itemCode = code;

                activeHUDItems.Add(newItem);
            }

            // 3. Cập nhật dữ liệu & Icon
            RefreshResources();
        }

        private void ClearActiveItems()
        {
            foreach (var item in activeHUDItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
            activeHUDItems.Clear();

            // Destroy any leftover children in the container to clean up static layout placeholders,
            // except the template/prefab itself if it is located inside the container.
            if (currencyContainer != null)
            {
                var toDestroy = new List<GameObject>();
                foreach (Transform child in currencyContainer)
                {
                    if (currencyItemPrefab != null && child.gameObject == currencyItemPrefab.gameObject)
                    {
                        continue;
                    }
                    toDestroy.Add(child.gameObject);
                }
                foreach (var go in toDestroy)
                {
                    Destroy(go);
                }
            }
        }

        private void OnBuildMenuClicked()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenPanel("UI_BuildMenuPanel", 0, false);
            }
        }

        private void OnInventoryClicked()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenPanel("UI_InventoryPanel", null, false);
            }
        }

        private void OnDisciplesClicked()
        {
            if (UIManager.Instance != null)
            {
                // Mở danh sách đệ tử
                UIManager.Instance.OpenPanel("UI_HeroesPanel", null, false);
            }
        }

        private async void OnWorldMapClicked()
        {
            if (MapManager.Instance != null)
            {
                await MapManager.Instance.LoadMapAsync(MapType.WorldMap);
            }
        }

        private void OnSettingsClicked()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenPanel("UI_SettingPanel", null, false);
            }
        }

        private void OnLeaderboardClicked()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenPanel("UI_LeaderboardPanel", null, false);
            }
        }

        private void OnShopClicked()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenPanel("UI_ShopPanel", null, false);
            }
        }

        public async void RefreshResources()
        {
            var player = GameManager.Instance.CurrentPlayer;
            
            currentResourceAmounts.Clear();
            // Khởi tạo mặc định số lượng cho các chỉ số và vật phẩm tài nguyên cơ bản
            currentResourceAmounts["gold"] = player != null ? (int)player.Gold : 0;
            currentResourceAmounts["qi"] = player != null ? (int)player.Diamond : 0;
            currentResourceAmounts["stamina"] = player != null ? (int)player.Stamina : 0; // Thể Lực lưu theo key 'stamina'
            currentResourceAmounts["max_stamina"] = player != null ? (int)player.MaxStamina : 100;
            currentResourceAmounts["00002"] = 0;
            currentResourceAmounts["00003"] = 0;

            try
            {
                var inventory = await GameClient.Network.Api.SectBuildingApi.GetInventoryAsync();
                if (inventory != null)
                {
                    // Tải và lưu các cấu hình tĩnh nhận được từ Server vào ItemDataManager
                    if (inventory.Configs != null)
                    {
                        ItemDataManager.Instance.LoadConfigs(inventory.Configs);
                    }

                    if (inventory.Items != null)
                    {
                        foreach (var item in inventory.Items)
                        {
                            if (item == null) continue;

                            // Tránh việc ghi đè các chỉ số tiền tệ/thể lực chính bằng các vật phẩm ảo trùng tên trong user_items
                            if (item.ItemCode == "stamina" || item.ItemCode == "gold" || item.ItemCode == "qi" || item.ItemCode == "coin")
                            {
                                continue;
                            }

                            // Lưu số lượng theo ItemCode gốc nhận từ Server
                            currentResourceAmounts[item.ItemCode] = (int)item.Quantity;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MainGameHUDPanel] Lỗi tải tài nguyên từ kho: {ex.Message}");
            }

            UpdateHUDDisplays();
        }

        private async void UpdateHUDDisplays()
        {
            foreach (var item in activeHUDItems)
            {
                if (item == null) continue;

                // 1. Lấy số lượng thực tế
                int quantity = 0;
                currentResourceAmounts.TryGetValue(item.itemCode, out quantity);

                // 2. Tải Sprite từ Addressable dựa theo cấu hình Icon của Server hoặc dùng fallback key
                Sprite sprite = null;
                var config = ItemDataManager.Instance.GetItemConfig(item.itemCode);
                string iconKey = "";
                if (config != null && !string.IsNullOrEmpty(config.Icon))
                {
                    iconKey = config.Icon;
                }
                else
                {
                    if (item.itemCode == "00000" || item.itemCode == "coin") iconKey = "coin_icon";
                    else if (item.itemCode == "00001" || item.itemCode == "gold") iconKey = "gold_icon";
                    else if (item.itemCode == "00002" || item.itemCode == "stone" || item.itemCode == "stone_1") iconKey = "stone_1_icon";
                    else if (item.itemCode == "00003" || item.itemCode == "wood" || item.itemCode == "wood_1") iconKey = "wood_1_icon";
                    else if (item.itemCode == "stamina") iconKey = "stamina_icon";
                    else iconKey = item.itemCode + "_icon";
                }

                try
                {
                    sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(iconKey);
                }
                catch (System.Exception)
                {
                    try
                    {
                        sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(item.itemCode);
                    }
                    catch (System.Exception) {}
                }

                // 3. Cập nhật dữ liệu hiển thị (Số lượng, Icon, Localization) lên UI Component
                string nameKey = "";
                if (config != null && !string.IsNullOrEmpty(config.NameKey))
                {
                    nameKey = config.NameKey;
                }
                else
                {
                    
                    // Tránh cho lỗi
                    if (item.itemCode == "00000" || item.itemCode == "coin") nameKey = "coin";
                    else if (item.itemCode == "00001" || item.itemCode == "gold") nameKey = "gold";
                    else if (item.itemCode == "00002" || item.itemCode == "stone" || item.itemCode == "stone_1") nameKey = "stone_1";
                    else if (item.itemCode == "00003" || item.itemCode == "wood" || item.itemCode == "wood_1") nameKey = "wood_1";
                    else if (item.itemCode == "stamina") nameKey = "stamina";
                    else nameKey = item.itemCode;
                }
                int maxQty = -1;
                if (item.itemCode == "stamina")
                {
                    currentResourceAmounts.TryGetValue("max_stamina", out maxQty);
                }
                item.UpdateData(quantity, sprite, nameKey, maxQty);
            }
        }

        protected override void OnCleanup()
        {
            base.OnCleanup();
            UnityEngine.Localization.Settings.LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerUpdated -= HandlePlayerUpdated;
            }
        }

        private void OnLocaleChanged(UnityEngine.Localization.Locale locale)
        {
            UpdateHUDDisplays();
        }

        private Button FindButtonByName(string name)
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                if (btn.gameObject.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return btn;
                }
                
                if (btn.transform.parent != null && btn.transform.parent.gameObject.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    return btn;
                }
            }
            return null;
        }
    }
}



