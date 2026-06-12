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

                // 2. Cập nhật thông tin và dịch thuật qua LocalizationManager từ bảng ITEM_EQUIPMENT
                if (txtItemName != null) txtItemName.text = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.ITEM_EQUIPMENT, config.NameKey);
                if (txtDescription != null) txtDescription.text = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.ITEM_EQUIPMENT, config.DescKey);
                if (txtItemType != null)
                {
                    string localizedType = TranslateItemType(config.Type);
                    txtItemType.text = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_item_type", localizedType);
                }
                
                // Cấp dùng từ cấu hình
                if (txtRequiredLevel != null)
                {
                    int reqLvl = config.RequiredLevel > 0 ? (int)config.RequiredLevel : 1;
                    txtRequiredLevel.text = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_item_req_level", reqLvl);
                }

                // 3. Hiển thị số lượng sở hữu thực tế của người chơi từ Inventory
                if (!hasCustomQuantity)
                {
                    ownedCount = GetOwnedQuantity(itemCode);
                }
                if (txtOwnedQuantity != null) txtOwnedQuantity.text = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_item_owned", ownedCount);

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
            string key = "ui_item_type_default";
            switch (rawType.ToUpper())
            {
                case "CONSUMABLE": key = "ui_item_type_consumable"; break;
                case "EQUIPMENT": key = "ui_item_type_equipment"; break;
                case "CURRENCY": key = "ui_item_type_currency"; break;
                case "SKIN_UNLOCKER": key = "ui_item_type_skin"; break;
            }
            string localized = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, key);
            if (string.IsNullOrEmpty(localized) || localized.StartsWith("["))
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
            return localized;
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

        private float _openTime;

        protected override void OnShow()
        {
            base.OnShow();
            _openTime = Time.time;
        }

        private void Update()
        {
            if (Time.time - _openTime < 0.15f) return;

            if (InputManager.Instance != null && InputManager.Instance.IsPrimaryPointerDown())
            {
                Vector2 pointerPos = InputManager.Instance.GetPointerPosition();
                RectTransform rectTransform = transform as RectTransform;
                if (rectTransform != null)
                {
                    var canvas = GetComponentInParent<Canvas>();
                    var cam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : Camera.main;
                    if (!RectTransformUtility.RectangleContainsScreenPoint(rectTransform, pointerPos, cam))
                    {
                        Hide();
                    }
                }
            }
        }
    }
}
