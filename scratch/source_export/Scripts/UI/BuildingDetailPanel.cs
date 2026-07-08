using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameClient.UI.Core;
using GameClient.Gameplay.BaseBuilder;
using GameClient.Network.Api;
using GameClient.Managers;
using GameClient.Core;
using System.Threading.Tasks;
using GameClient.BaseBuilding.Core;

namespace GameClient.UI
{
    public class BuildingDetailPanel : BaseUIPanel
    {
        [Header("Identity UI")]
        [SerializeField] private TMP_Text txtBuildingName;
        [SerializeField] private TMP_Text txtBuildingDesc;
        [SerializeField] private TMP_Text txtBuildingLevel;
        [SerializeField] private Image imgBuildingIcon;

        [Header("Upgrade Cost Info")]
        [SerializeField] private TMP_Text txtUpgradeTime;
        [SerializeField] private GameObject costContainer;
        [SerializeField] private TMP_Text txtMaxLevelReached;
        [SerializeField] private GameObject upgradeTimeContainer; // Container chứa text time và icon đồng hồ cát

        [Header("Dynamic Sub-Panel Configuration")]
        [SerializeField] private Transform subPanelContainer; // Vùng chứa Sub-Panel
        [SerializeField] private GameObject defaultSubPanelPrefab; // Prefab mặc định dự phòng

        [Header("Control Buttons")]
        [SerializeField] private Button btnUpgrade;
        [SerializeField] private Button btnClose;

        private BuildingInstance _currentBuilding;
        private GameObject _activeSubPanelInstance;
        private float _previousZoom = 2.0f;
        private bool _hasSavedZoom = false;

        protected override void Awake()
        {
            base.Awake();
            
            if (btnUpgrade != null) btnUpgrade.onClick.AddListener(OnUpgradeClicked);
            if (btnClose != null) btnClose.onClick.AddListener(Hide);
        }

        public override void Setup(object data)
        {
            base.Setup(data);
            if (!_hasSavedZoom && CameraController.Instance != null)
            {
                _previousZoom = CameraController.Instance.CurrentZoom;
                _hasSavedZoom = true;
            }
            
            if (data is BuildingInstance building)
            {
                _currentBuilding = building;
                RefreshUI();
            }
            else
            {
                Debug.LogError("[BuildingDetailPanel] Dữ liệu truyền vào không khớp!");
                Hide();
            }
        }

        private void RefreshUI()
        {
            if (_currentBuilding == null || _currentBuilding.Data == null) return;

            string localizedName = LocalizationManager.Instance.GetText(_currentBuilding.Data.BuildingNameKey);
            if (string.IsNullOrEmpty(localizedName) || localizedName.StartsWith("[")) localizedName = _currentBuilding.Data.BuildingNameKey;

            if (txtBuildingName != null) txtBuildingName.text = localizedName;
            if (txtBuildingLevel != null) txtBuildingLevel.text = $"Lv.{_currentBuilding.CurrentLevel}";
            
            if (txtBuildingDesc != null)
            {
                txtBuildingDesc.text = GetBuildingDescription(_currentBuilding.Data.Type, _currentBuilding.Data.BuildingID);
            }

            if (imgBuildingIcon != null && _currentBuilding.Data.VisualConfig != null)
            {
                var vis = _currentBuilding.Data.VisualConfig.GetVisualsForLevel(_currentBuilding.CurrentLevel);
                if (vis != null && vis.normalSprite != null)
                {
                    imgBuildingIcon.sprite = vis.normalSprite;
                    imgBuildingIcon.preserveAspect = true;
                }
            }

            _ = LoadSubPanelAsync();

            UpdateUpgradeRequirements();
        }

        private async Task LoadSubPanelAsync()
        {
            if (_activeSubPanelInstance != null)
            {
                Destroy(_activeSubPanelInstance);
                _activeSubPanelInstance = null;
            }

            if (subPanelContainer == null) return;

            string subPanelAddress = _currentBuilding.Data.DetailSubPanelAddress;
            GameObject subPanelGo = null;

            if (!string.IsNullOrEmpty(subPanelAddress))
            {
                try
                {
                    subPanelGo = await ResourceManager.Instance.InstantiateAsync(subPanelAddress, subPanelContainer);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[BuildingDetailPanel] Không load được SubPanel qua Addressable tại {subPanelAddress}: {ex.Message}");
                }
            }

            if (subPanelGo == null && defaultSubPanelPrefab != null)
            {
                subPanelGo = Instantiate(defaultSubPanelPrefab, subPanelContainer);
            }

            if (subPanelGo != null)
            {
                _activeSubPanelInstance = subPanelGo;
                
                var subPanel = subPanelGo.GetComponent<BuildingSubPanel>();
                if (subPanel != null)
                {
                    subPanel.Setup(_currentBuilding);
                }
            }
        }

        [Header("Dynamic Upgrade Costs")]
        [SerializeField] private HUDCurrencyItem costItemPrefab; // Drag HUDCurrencyItem prefab here
        [SerializeField] private Transform costItemsContainer; // Grid / Horizontal Layout Group inside Cost Container or Cost Container itself

        private List<HUDCurrencyItem> _spawnedCostItems = new List<HUDCurrencyItem>();

        private async Task<int> GetCurrentItemQuantity(string itemCode)
        {
            if (string.IsNullOrEmpty(itemCode)) return 0;
            
            // Get from player's inventory
            try
            {
                var inventory = await GameClient.Network.Api.SectBuildingApi.GetInventoryAsync();
                if (inventory != null && inventory.Items != null)
                {
                    foreach (var item in inventory.Items)
                    {
                        if (item.ItemCode == itemCode)
                        {
                            return (int)item.Quantity;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BuildingDetailPanel] Lỗi tải tài nguyên từ kho: {ex.Message}");
            }
            return 0;
        }

        private async void UpdateUpgradeRequirements()
        {
            // Clear old spawned items first
            foreach (var item in _spawnedCostItems)
            {
                if (item != null) Destroy(item.gameObject);
            }
            _spawnedCostItems.Clear();

            int nextLevel = _currentBuilding.CurrentLevel + 1;
            var levelStatsList = _currentBuilding.Data.LevelStats;
            BuildingLevelStats nextLevelStats = levelStatsList.Find(s => s.Level == nextLevel);

            if (nextLevelStats != null)
            {
                if (costContainer != null) costContainer.SetActive(true);
                if (upgradeTimeContainer != null) upgradeTimeContainer.SetActive(true);
                else if (txtUpgradeTime != null && txtUpgradeTime.transform.parent != null) txtUpgradeTime.transform.parent.gameObject.SetActive(true);
                
                if (txtMaxLevelReached != null) txtMaxLevelReached.gameObject.SetActive(false);

                // Nút Nâng cấp bị vô hiệu hóa nếu công trình đang trong quá trình nâng cấp/xây dựng
                bool isUpgrading = _currentBuilding.CurrentState == BuildingState.Building || _currentBuilding.CurrentState == BuildingState.Upgrading;
                if (btnUpgrade != null)
                {
                    btnUpgrade.gameObject.SetActive(true);
                    btnUpgrade.interactable = !isUpgrading;
                }

                if (txtUpgradeTime != null) txtUpgradeTime.text = $"{nextLevelStats.BuildTimeSeconds} giây";

                Debug.Log($"[BuildingDetailPanel] Update requirements: Level={nextLevel}, CostItemsCount={nextLevelStats.CostItems?.Count ?? 0}, costItemPrefab={(costItemPrefab != null ? "Assigned" : "Null")}, costItemsContainer={(costItemsContainer != null ? "Assigned" : "Null")}");

                // Hiển thị chi phí nâng cấp từ cấu hình CostItems
                if (nextLevelStats.CostItems != null && costItemPrefab != null && costItemsContainer != null)
                {
                    foreach (var cost in nextLevelStats.CostItems)
                    {
                        if (string.IsNullOrEmpty(cost.ItemCode)) continue;

                        HUDCurrencyItem newItem = Instantiate(costItemPrefab, costItemsContainer);
                        newItem.gameObject.SetActive(true);
                        newItem.itemCode = cost.ItemCode;
                        _spawnedCostItems.Add(newItem);

                        // Fetch inventory count
                        int ownedQty = await GetCurrentItemQuantity(cost.ItemCode);

                        // Load sprite from Addressable
                        Sprite sprite = null;
                        var config = ItemDataManager.Instance.GetItemConfig(cost.ItemCode);
                        string iconKey = "";
                        if (config != null && !string.IsNullOrEmpty(config.Icon))
                        {
                            iconKey = config.Icon;
                        }
                        else
                        {
                            if (cost.ItemCode == "00000" || cost.ItemCode == "coin") iconKey = "coin_icon";
                            else if (cost.ItemCode == "00002" || cost.ItemCode == "stone" || cost.ItemCode == "stone_1") iconKey = "stone_1_icon";
                            else if (cost.ItemCode == "00003" || cost.ItemCode == "wood" || cost.ItemCode == "wood_1") iconKey = "wood_1_icon";
                            else if (cost.ItemCode == "gold") iconKey = "gold_icon";
                            else iconKey = cost.ItemCode + "_icon";
                        }

                        try
                        {
                            sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(iconKey);
                        }
                        catch (System.Exception)
                        {
                            try
                            {
                                sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(cost.ItemCode);
                            }
                            catch (System.Exception) {}
                        }

                        string nameKey = "";
                        if (config != null && !string.IsNullOrEmpty(config.NameKey))
                        {
                            nameKey = config.NameKey;
                        }
                        else
                        {
                            if (cost.ItemCode == "00000" || cost.ItemCode == "coin") nameKey = "coin";
                            else if (cost.ItemCode == "00002" || cost.ItemCode == "stone" || cost.ItemCode == "stone_1") nameKey = "stone_1";
                            else if (cost.ItemCode == "00003" || cost.ItemCode == "wood" || cost.ItemCode == "wood_1") nameKey = "wood_1";
                            else if (cost.ItemCode == "gold") nameKey = "gold";
                            else nameKey = cost.ItemCode;
                        }
                        
                        newItem.UpdateData(0, sprite, nameKey); // Temp call to load icon & name

                        // Set quantity text custom format: owned/required
                        if (newItem.txtQuantity != null)
                        {
                            newItem.txtQuantity.text = $"{ownedQty}/{cost.Quantity}";
                            if (ownedQty < cost.Quantity)
                            {
                                newItem.txtQuantity.color = Color.red;
                            }
                            else
                            {
                                newItem.txtQuantity.color = Color.white; // Or any default color
                            }
                        }
                    }
                }
            }
            else
            {
                if (costContainer != null) costContainer.SetActive(false);
                if (upgradeTimeContainer != null) upgradeTimeContainer.SetActive(false);
                else if (txtUpgradeTime != null && txtUpgradeTime.transform.parent != null) txtUpgradeTime.transform.parent.gameObject.SetActive(false);
                
                if (txtMaxLevelReached != null)
                {
                    txtMaxLevelReached.gameObject.SetActive(true);
                    txtMaxLevelReached.text = "Đã đạt cấp độ tối đa";
                }
                if (btnUpgrade != null) btnUpgrade.gameObject.SetActive(false);
            }
        }

        private string GetBuildingDescription(BuildingType type, string buildingId)
        {
            string id = buildingId.ToLower();
            if (id.Contains("residence")) return "Nơi đệ tử cư ngụ và phục hồi thể lực sau những giờ tu luyện, làm việc mệt mỏi.";
            if (id.Contains("farm")) return "Nơi canh tác linh thực, cung cấp nguồn lương thực linh khí dồi dào cho tông môn.";
            if (id.Contains("alchemy")) return "Lò luyện đan chuyên dụng để điều chế linh đan bồi dưỡng tu vi đệ tử.";
            
            switch (type)
            {
                case BuildingType.MainHall:
                    return "Trung tâm đầu não của Tông Môn. Nâng cấp để mở khóa các công trình mới và gia tăng lãnh thổ.";
                case BuildingType.Resource:
                    return "Nơi khai thác hoặc cất trữ linh thạch, tài nguyên gỗ phục vụ cho việc xây dựng Tông Môn.";
                case BuildingType.Military:
                    return "Khu vực chiêu mộ và rèn luyện đệ tử của Tông Môn, gia tăng sức mạnh quân sự.";
                default:
                    return "Một công trình trang trí giúp Tông Môn thêm uy nghiêm và đẹp mắt.";
            }
        }

        private async void OnUpgradeClicked()
        {
            if (_currentBuilding == null) return;
            if (btnUpgrade != null) btnUpgrade.interactable = false;

            try
            {
                long instanceId = _currentBuilding.InstanceID;
                var resp = await SectBuildingApi.UpgradeBuildingAsync(instanceId);
                
                if (resp != null && resp.Base != null && resp.Base.Code == 0)
                {
                    string buildingName = LocalizationManager.Instance.GetText(_currentBuilding.Data.BuildingNameKey); if (string.IsNullOrEmpty(buildingName) || buildingName.StartsWith("[")) buildingName = _currentBuilding.Data.BuildingNameKey; int targetLevel = _currentBuilding.CurrentLevel + 1; string successTitle = LocalizationManager.Instance.GetText("UI_System", "ui_success_title") ?? "Thành Công"; string successMsgFormat = LocalizationManager.Instance.GetText("UI_System", "ui_building_upgrade_start_success") ?? "Đã bắt đầu nâng cấp {0} lên Cấp {1}!"; GameClient.UIManager.Instance.ShowMessage(successTitle, string.Format(successMsgFormat, buildingName, targetLevel));
                    
                    var baseResp = await SectBuildingApi.GetBaseAsync();
                    if (baseResp != null && baseResp.Base != null && baseResp.Base.Code == 0)
                    {
                        GameClient.GameManager.Instance.SetBuildings(baseResp.Buildings);
                        BaseBuildingManager.Instance.SyncBuildingsWithServerData(baseResp.Buildings);
                    }
                    Hide();
                }
                else
                {
                    string errorMsg = resp?.Base?.Message ?? "Lỗi không xác định từ Server";
                    GameClient.UIManager.Instance.ShowMessage("Lỗi Nâng Cấp", errorMsg);
                }
            }
            catch (System.Exception ex)
            {
                GameClient.UIManager.Instance.ShowMessage("Lỗi Nâng Cấp", ex.Message);
            }
            finally
            {
                if (btnUpgrade != null) btnUpgrade.interactable = true;
            }
        }

        public override void Hide()
        {
            base.Hide();
            if (CameraController.Instance != null && _hasSavedZoom)
            {
                CameraController.Instance.FocusZoom(_previousZoom, 0.5f);
            }
            _hasSavedZoom = false;
        }
    }
}
