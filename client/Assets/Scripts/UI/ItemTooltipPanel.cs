using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameClient.UI.Core;
using GameClient.Managers;
using GameClient.Core;
using GameClient.Network.Pb; // Nơi định nghĩa ItemConfig từ protobuf

namespace GameClient.UI
{
    public class ItemTooltipPanel : BaseUIPanel
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text txtItemName;
        [SerializeField] private TMP_Text txtItemType;
        [SerializeField] private TMP_Text txtRequiredLevel;
        [SerializeField] private TMP_Text txtDescription;
        [SerializeField] private TMP_Text txtOwnedQuantity;
        [SerializeField] private Image imgItemIcon;
        [SerializeField] private Image imgBackgroundFrame; // Khung màu tương ứng với phẩm chất (Rarity)
        [SerializeField] private Button btnCloseArea; // Nút vô hình phủ toàn màn hình để click ra ngoài là tắt popup

        protected override void OnInit()
        {
            base.OnInit();
            if (btnCloseArea != null)
            {
                btnCloseArea.onClick.AddListener(Hide);
            }
        }

        public override void Setup(object data)
        {
            base.Setup(data);

            string itemCode = "";
            int ownedCount = 0;
            bool hasCustomQuantity = false;

            if (data is string code)
            {
                itemCode = code;
            }
            else if (data is System.Tuple<string, int> tuple)
            {
                itemCode = tuple.Item1;
                ownedCount = tuple.Item2;
                hasCustomQuantity = true;
            }
            else if (data is System.ValueTuple<string, int> vtuple)
            {
                itemCode = vtuple.Item1;
                ownedCount = vtuple.Item2;
                hasCustomQuantity = true;
            }

            if (!string.IsNullOrEmpty(itemCode))
            {
                // 1. Lấy dữ liệu cấu hình vật phẩm từ ItemDataManager
                var config = ItemDataManager.Instance.GetItemConfig(itemCode);
                if (config == null)
                {
                    Debug.LogWarning($"[ItemTooltip] Không tìm thấy cấu hình cho item: {itemCode}");
                    Hide();
                    return;
                }

                // 2. Cập nhật thông tin và dịch thuật qua LocalizationManager
                if (txtItemName != null) txtItemName.text = LocalizationManager.Instance.GetText(config.NameKey);
                if (txtDescription != null) txtDescription.text = LocalizationManager.Instance.GetText(config.DescKey);
                if (txtItemType != null) txtItemType.text = $"Loại: {TranslateItemType(config.Type)}";
                
                // Mặc định cấp dùng
                if (txtRequiredLevel != null) txtRequiredLevel.text = "Cấp dùng: Tông Môn Cấp 1"; 

                // 3. Hiển thị số lượng sở hữu thực tế của người chơi từ Inventory
                if (!hasCustomQuantity)
                {
                    ownedCount = GetOwnedQuantity(itemCode);
                }
                if (txtOwnedQuantity != null) txtOwnedQuantity.text = $"Sở hữu: <color=#00FF00>{ownedCount}</color>";

                // 4. Load ảnh icon vật phẩm từ Addressable
                LoadIconAsync(config, itemCode);

                // 5. Thay đổi màu sắc khung/chữ tiêu đề dựa theo phẩm chất (Rarity)
                ApplyRarityColor(config.Rarity);
            }
        }

        private async void LoadIconAsync(ItemConfig config, string itemCode)
        {
            if (imgItemIcon == null) return;

            Sprite sprite = null;
            string iconKey = config != null && !string.IsNullOrEmpty(config.Icon) ? config.Icon : itemCode + "_icon";

            try
            {
                sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(iconKey);
            }
            catch (System.Exception)
            {
                try
                {
                    sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(itemCode);
                }
                catch (System.Exception) {}
            }

            if (sprite == null)
            {
                // Fallback to legacy Resources load
                sprite = Resources.Load<Sprite>($"Sprites/Items/{iconKey}");
            }

            if (imgItemIcon != null && sprite != null)
            {
                imgItemIcon.sprite = sprite;
            }
        }

        private int GetOwnedQuantity(string itemCode)
        {
            return 1; 
        }

        private string TranslateItemType(string rawType)
        {
            switch (rawType.ToUpper())
            {
                case "CONSUMABLE": return "Đạo Cụ";
                case "EQUIPMENT": return "Trang Bị";
                case "CURRENCY": return "Tiền Tệ";
                case "SKIN_UNLOCKER": return "Ngoại Trang";
                default: return "Vật Phẩm";
            }
        }

        private void ApplyRarityColor(string rarity)
        {
            if (txtItemName == null) return;
            
            // Đổi màu tiêu đề vật phẩm theo phẩm chất (Rarity)
            switch (rarity.ToUpper())
            {
                case "LEGENDARY": // Đỏ/Cam
                    txtItemName.color = new Color(1f, 0.3f, 0.3f);
                    break;
                case "EPIC": // Tím
                    txtItemName.color = new Color(0.7f, 0.3f, 1f);
                    break;
                case "RARE": // Lam
                    txtItemName.color = new Color(0.2f, 0.6f, 1f);
                    break;
                default: // Trắng/Lục
                    txtItemName.color = Color.white;
                    break;
            }
        }
    }
}
