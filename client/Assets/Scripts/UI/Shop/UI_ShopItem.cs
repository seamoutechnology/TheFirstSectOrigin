using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GameClient.UI
{
    public class UI_ShopItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text txtName;
        [SerializeField] private TMP_Text txtPrice;
        [SerializeField] private Image imgIcon;
        [SerializeField] private Button btnBuy;

        public string ItemId { get; private set; }
        public int Price { get; private set; }
        public string CurrencyType { get; private set; } // "gold" or "diamond"

        private System.Action<UI_ShopItem> onBuyClicked;

        public void Setup(string itemId, string itemName, int price, string currencyType, Sprite iconSprite, System.Action<UI_ShopItem> buyHandler)
        {
            ItemId = itemId;
            Price = price;
            CurrencyType = currencyType;
            onBuyClicked = buyHandler;

            if (txtName != null) txtName.text = itemName;
            if (txtPrice != null) txtPrice.text = price.ToString();
            if (imgIcon != null && iconSprite != null) imgIcon.sprite = iconSprite;

            if (btnBuy != null)
            {
                btnBuy.onClick.RemoveAllListeners();
                btnBuy.onClick.AddListener(() => onBuyClicked?.Invoke(this));
            }
        }
    }
}
