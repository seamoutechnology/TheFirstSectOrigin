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
        
        [Header("Optional References")]
        [SerializeField] private GameObject discountBadge;
        [SerializeField] private TMP_Text txtDiscountPct;
        [SerializeField] private GameObject boughtOverlay;

        public long InstanceId { get; private set; }
        public string ItemCode { get; private set; }
        public int PriceAmount { get; private set; }
        public string CurrencyType { get; private set; }
        public int DiscountPct { get; private set; }
        public bool IsBought { get; private set; }

        private System.Action<UI_ShopItem> onBuyClicked;

        public void Setup(long instanceId, string itemCode, string itemName, int priceAmount, string currencyType, int discountPct, bool isBought, Sprite iconSprite, System.Action<UI_ShopItem> buyHandler)
        {
            InstanceId = instanceId;
            ItemCode = itemCode;
            PriceAmount = priceAmount;
            CurrencyType = currencyType;
            DiscountPct = discountPct;
            IsBought = isBought;
            onBuyClicked = buyHandler;

            if (txtName != null) txtName.text = itemName;
            
            // Format price: e.g. "100 Gold" or "50 Diamond"
            if (txtPrice != null)
            {
                txtPrice.text = $"{priceAmount} {currencyType}";
            }

            if (imgIcon != null && iconSprite != null)
            {
                imgIcon.sprite = iconSprite;
            }

            // Discount rendering
            if (discountBadge != null)
            {
                discountBadge.SetActive(discountPct > 0);
            }
            if (txtDiscountPct != null && discountPct > 0)
            {
                txtDiscountPct.text = $"-{discountPct}%";
            }

            // Bought state overlay and button interactability
            if (boughtOverlay != null)
            {
                boughtOverlay.SetActive(isBought);
            }
            if (btnBuy != null)
            {
                btnBuy.interactable = !isBought;
                btnBuy.onClick.RemoveAllListeners();
                btnBuy.onClick.AddListener(() => onBuyClicked?.Invoke(this));
            }
        }

        public void SetIcon(Sprite sprite)
        {
            if (imgIcon != null && sprite != null)
            {
                imgIcon.sprite = sprite;
            }
        }
    }
}
