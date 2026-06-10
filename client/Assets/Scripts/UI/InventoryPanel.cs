using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameClient.UI.Core;
using GameClient.Managers;
using GameClient.Core;
using GameClient.Network.Api;

namespace GameClient.UI
{
    public class InventoryPanel : BaseUIPanel
    {
        [Header("UI Grid References")]
        [SerializeField] private Transform gridContainer;
        [SerializeField] private GameObject itemSlotPrefab; // Prefab UI_InventoryItem mẫu kéo trong Editor

        [Header("Buttons")]
        [SerializeField] private Button btnClose;

        private string _selectedItemCode = "";

        protected override void OnStart()
        {
            base.OnStart();
            if (btnClose != null)
            {
                btnClose.onClick.AddListener(Hide);
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            
            // Tạm ẩn HUD khi vào kho đồ để thoáng màn hình
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ClosePanel("MainGameHUDPanel");
            }

            _selectedItemCode = "";

            _ = LoadInventoryAsync();
        }

        protected override void OnHide()
        {
            base.OnHide();
            
            // Hiện lại HUD khi thoát kho đồ
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenPanel("MainGameHUDPanel");
            }
        }

        private async Task LoadInventoryAsync()
        {
            // Xóa sạch các slot cũ đang hiển thị
            foreach (Transform child in gridContainer)
            {
                Destroy(child.gameObject);
            }

            try
            {
                var inventory = await SectBuildingApi.GetInventoryAsync();
                if (inventory == null || inventory.Items == null || inventory.Items.Count == 0)
                {
                    Debug.Log("[InventoryPanel] Túi đồ trống!");
                    return;
                }

                // Cập nhật ItemDataManager để có config mới nhất
                if (inventory.Configs != null)
                {
                    ItemDataManager.Instance.LoadConfigs(inventory.Configs);
                }

                foreach (var item in inventory.Items)
                {
                    if (item == null || item.Quantity <= 0) continue;

                    var slotGo = Instantiate(itemSlotPrefab, gridContainer);
                    slotGo.SetActive(true);

                    var slotScript = slotGo.GetComponent<UI_InventoryItem>();
                    if (slotScript != null)
                    {
                        string code = item.ItemCode;
                        int qty = (int)item.Quantity;
                        slotScript.Setup(code, qty, () => ShowItemDetail(code, qty));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[InventoryPanel] Lỗi khi nạp túi đồ: {ex.Message}");
            }
        }

        private void ShowItemDetail(string itemCode, int quantity)
        {
            _selectedItemCode = itemCode;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenPanel("UI_ItemTooltipPanel", (itemCode, quantity), false);
            }
        }
    }
}
