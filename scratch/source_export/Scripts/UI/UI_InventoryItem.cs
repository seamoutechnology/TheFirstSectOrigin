using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using GameClient.Managers;
using GameClient.Core;

namespace GameClient.UI
{
    public class UI_InventoryItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image imgIcon;
        [SerializeField] private Image imgBorder;
        [SerializeField] private TMP_Text txtQuantity;
        [SerializeField] private Button btnSelect;

        private string _itemCode;
        private int _quantity;
        private Action _onClick;

        private void Start()
        {
            if (btnSelect != null)
            {
                btnSelect.onClick.RemoveAllListeners();
                btnSelect.onClick.AddListener(() => _onClick?.Invoke());
            }
        }

        public async void Setup(string itemCode, int quantity, Action onClick)
        {
            _itemCode = itemCode;
            _quantity = quantity;
            _onClick = onClick;

            if (txtQuantity != null)
            {
                txtQuantity.text = GameClient.Utils.NumberUtils.FormatNumber(quantity);
            }

            // Lấy config của vật phẩm để biết Rarity và Icon
            var config = ItemDataManager.Instance.GetItemConfig(itemCode);
            
            // 1. Tải Sprite Icon từ Addressable
            Sprite sprite = null;
            string iconKey = "";
            if (config != null && !string.IsNullOrEmpty(config.Icon))
            {
                iconKey = config.Icon;
            }
            else
            {
                // Fallbacks cơ bản
                if (itemCode == "00000" || itemCode == "coin") iconKey = "coin_icon";
                else if (itemCode == "00002" || itemCode == "stone" || itemCode == "stone_1") iconKey = "stone_1_icon";
                else if (itemCode == "00003" || itemCode == "wood" || itemCode == "wood_1") iconKey = "wood_1_icon";
                else if (itemCode == "gold") iconKey = "gold_icon";
                else iconKey = itemCode + "_icon";
            }

            try
            {
                sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(iconKey);
            }
            catch (Exception)
            {
                try
                {
                    sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(itemCode);
                }
                catch (Exception) {}
            }

            if (imgIcon != null && sprite != null)
            {
                imgIcon.sprite = sprite;
            }

            // 2. Thiết lập màu viền dựa theo độ hiếm (Rarity)
            if (imgBorder != null)
            {
                Color borderColor = Color.white; // Common
                if (config != null)
                {
                    switch (config.Rarity.ToUpper())
                    {
                        case "UNCOMMON":
                            borderColor = new Color(0.12f, 0.73f, 0.61f); // Teal/Green
                            break;
                        case "RARE":
                            borderColor = new Color(0.2f, 0.6f, 0.86f); // Blue
                            break;
                        case "EPIC":
                            borderColor = new Color(0.6f, 0.35f, 0.71f); // Purple
                            break;
                        case "LEGENDARY":
                            borderColor = new Color(0.9f, 0.5f, 0.15f); // Orange/Gold
                            break;
                    }
                }
                imgBorder.color = borderColor;
            }
        }
    }
}
