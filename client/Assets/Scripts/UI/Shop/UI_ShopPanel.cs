using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using GameClient.UI.Core;
using GameClient.Managers;
using GameClient.Core;

namespace GameClient.UI
{
    public class UI_ShopPanel : BaseUIPanel
    {
        [Header("Cấu hình Prefab")]
        [SerializeField] private UI_ShopItem itemPrefab;
        [SerializeField] private Transform contentContainer;
        [SerializeField] private Button btnClose;

        [System.Serializable]
        public struct ShopItemData
        {
            public string itemId;
            public string itemName;
            public int price;
            public string currencyType; // "gold" hoặc "diamond"
            public string iconKey;
        }

        [Header("Danh sách vật phẩm bán")]
        [SerializeField] private List<ShopItemData> shopItems = new List<ShopItemData>();

        private List<UI_ShopItem> spawnedItems = new List<UI_ShopItem>();

        protected override void OnStart()
        {
            base.OnStart();

            if (btnClose != null)
            {
                btnClose.onClick.AddListener(ClosePanel);
            }

            if (itemPrefab != null)
            {
                itemPrefab.gameObject.SetActive(false); // Ẩn mẫu gốc
            }

            // Nếu danh sách bán trống, tự động thêm một số mẫu test
            if (shopItems.Count == 0)
            {
                shopItems.Add(new ShopItemData { itemId = "recruit_ticket", itemName = "Vé Chiêu Mộ", price = 100, currencyType = "diamond", iconKey = "recruit_ticket_icon" });
                shopItems.Add(new ShopItemData { itemId = "stamina_potion", itemName = "Dược Thể Lực", price = 50, currencyType = "diamond", iconKey = "stamina_potion_icon" });
                shopItems.Add(new ShopItemData { itemId = "speed_hourglass", itemName = "Đồng Hồ Cát", price = 30, currencyType = "gold", iconKey = "speed_hourglass_icon" });
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            RenderShop();
        }

        private async void RenderShop()
        {
            ClearShop();

            foreach (var item in shopItems)
            {
                UI_ShopItem newCard = Instantiate(itemPrefab, contentContainer);
                newCard.gameObject.SetActive(true);

                // Load icon động từ Addressables
                Sprite iconSprite = null;
                try
                {
                    iconSprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(item.iconKey);
                }
                catch
                {
                    // Fallback nếu thiếu asset
                }

                newCard.Setup(item.itemId, item.itemName, item.price, item.currencyType, iconSprite, OnBuyItemClicked);
                spawnedItems.Add(newCard);
            }
        }

        private void OnBuyItemClicked(UI_ShopItem itemCard)
        {
            var player = GameManager.Instance.CurrentPlayer;
            if (player == null) return;

            string failedTitle = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_shop_failed") ?? "Thất Bại";
            string successTitle = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_shop_success") ?? "Thành Công";
            string buySuccessMsg = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_shop_buy_success") ?? "Mua thành công!";

            // Kiểm tra loại tiền tệ để mua
            if (itemCard.CurrencyType == "gold")
            {
                if (player.Gold < itemCard.Price)
                {
                    string noGoldMsg = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_shop_not_enough_gold") ?? "Không đủ Vàng để mua vật phẩm này!";
                    UIManager.Instance.ShowMessage(failedTitle, noGoldMsg);
                    return;
                }
                
                // MOCK giao dịch: Trừ tiền và thông báo thành công
                UIManager.Instance.ShowMessage(successTitle, $"{buySuccessMsg} (Mock)");
            }
            else // diamond / xu
            {
                if (player.Diamond < itemCard.Price)
                {
                    string noXuMsg = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_shop_not_enough_xu") ?? "Không đủ Xu để mua vật phẩm này!";
                    UIManager.Instance.ShowMessage(failedTitle, noXuMsg);
                    return;
                }

                UIManager.Instance.ShowMessage(successTitle, $"{buySuccessMsg} (Mock)");
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
