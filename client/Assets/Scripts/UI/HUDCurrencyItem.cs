using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameClient.Managers;
using GameClient.Core;

namespace GameClient.UI
{
    public class HUDCurrencyItem : MonoBehaviour
    {
        [Header("UI References")]
        public Image imgIcon;
        public TMP_Text txtQuantity;
        public TMP_Text txtName; // Text hiển thị tên vật phẩm (nếu UI cần hiện tên)
        
        [Header("Settings")]
        public string itemCode; // Định danh loại tài nguyên (ví dụ: "gold", "wood")

        /// <summary>
        /// Cập nhật hiển thị số lượng, icon và tên được dịch hóa cho tài nguyên
        /// </summary>
        public void UpdateData(int quantity, Sprite iconSprite = null, string nameKey = "")
        {
            if (txtQuantity != null)
            {
                txtQuantity.text = quantity.ToString();
            }

            if (imgIcon != null && iconSprite != null)
            {
                imgIcon.sprite = iconSprite;
            }

            if (txtName != null && !string.IsNullOrEmpty(nameKey))
            {
                // Lấy tên vật phẩm từ bảng Item_Equipment của Server
                string localizedName = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.ITEM_EQUIPMENT, nameKey);
                
                // Fallback nếu không dịch được
                if (string.IsNullOrEmpty(localizedName) || localizedName.StartsWith("["))
                {
                    localizedName = itemCode;
                }
                txtName.text = localizedName;
            }
        }
    }
}

