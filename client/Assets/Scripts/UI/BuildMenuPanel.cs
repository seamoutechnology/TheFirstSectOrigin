using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using GameClient.UI.Core;
using GameClient.Core;
using GameClient.Gameplay.BaseBuilder;
using GameClient.Managers;

namespace GameClient.UI
{
    public class BuildMenuPanel : BaseUIPanel
    {
        [Header("UI References")]
        [SerializeField] private Transform _buildingListContainer;
        [SerializeField] private string _buildingItemPrefabAddress = "UI_BuildingItem"; // Address của Item mẫu
        [SerializeField] private Button _closeButton;

        [Header("Category Tabs (Left Sidebar - Gán tay trong Inspector)")]
        [SerializeField] private Button _btnTabProduction;
        [SerializeField] private Button _btnTabFacility;
        [SerializeField] private Button _btnTabShop;
        [SerializeField] private Button _btnTabScenery;
        [SerializeField] private Button _btnTabInventory;

        private int _activeTab = 0; // 0: Sản xuất, 1: Cơ sở, 2: Cửa Hiệu, 3: Phong Cảnh, 4: Sở hữu
        private GameObject _tabHeaderGO;
        
        private readonly string[] _tabLocaleKeys = {
            "ui_build_tab_production",
            "ui_build_tab_facility",
            "ui_build_tab_shop",
            "ui_build_tab_scenery",
            "ui_build_tab_inventory"
        };

        private System.Collections.Generic.List<Button> _tabButtons = new System.Collections.Generic.List<Button>();
        private System.Collections.Generic.List<TMPro.TextMeshProUGUI> _tabTexts = new System.Collections.Generic.List<TMPro.TextMeshProUGUI>();

        private string GetLocalizedTabName(int index, int ownedCount = 0)
        {
            if (index < 0 || index >= _tabLocaleKeys.Length) return "";

            string key = _tabLocaleKeys[index];
            string loc = LocalizationManager.Instance != null 
                ? LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.UI_SYSTEM, key) 
                : key;

            if (string.IsNullOrEmpty(loc) || loc.StartsWith("["))
            {
                loc = key; // Fallback bằng chính key nếu không tìm thấy dịch
            }

            if (index == 4 && ownedCount > 0)
            {
                return $"{loc} ({ownedCount})";
            }

            return loc;
        }

        public override void Setup(object data)
        {
            base.Setup(data);
            if (data is int initialTab)
            {
                _activeTab = initialTab;
            }
            else
            {
                _activeTab = 0;
            }
        }

        protected override void OnStart()
        {
            base.OnStart();
            if (_closeButton != null) _closeButton.onClick.AddListener(Hide);

            if (_btnTabProduction != null) _btnTabProduction.onClick.AddListener(() => SwitchTab(0));
            if (_btnTabFacility != null) _btnTabFacility.onClick.AddListener(() => SwitchTab(1));
            if (_btnTabShop != null) _btnTabShop.onClick.AddListener(() => SwitchTab(2));
            if (_btnTabScenery != null) _btnTabScenery.onClick.AddListener(() => SwitchTab(3));
            if (_btnTabInventory != null) _btnTabInventory.onClick.AddListener(() => SwitchTab(4));
        }

        protected override void OnShow()
        {
            base.OnShow();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ClosePanel("MainGameHUDPanel");
            }

            if (_buildingListContainer == null)
            {
                var scrollRect = GetComponentInChildren<ScrollRect>();
                if (scrollRect != null && scrollRect.content != null)
                {
                    _buildingListContainer = scrollRect.content;
                }
                else
                {
                    var found = transform.Find("Scroll View/Viewport/Content") ?? 
                                transform.Find("MainPanel/ScrollRect/Viewport/Content") ??
                                transform.Find("ScrollRect/Viewport/Content") ??
                                transform.Find("Content");
                    if (found != null)
                    {
                        _buildingListContainer = found;
                    }
                }
            }

            if (_btnTabProduction == null && _btnTabFacility == null)
            {
                CreateTabHeaderIfNeeded();
            }
            
            UpdateTabVisuals();
            _ = PopulateBuildingListAsync();
        }

        protected override void OnHide()
        {
            base.OnHide();
            if (UIManager.Instance != null)
            {
                UIManager.Instance.OpenPanel("MainGameHUDPanel");
            }
        }

        private void CreateTabHeaderIfNeeded()
        {
            if (_tabHeaderGO != null)
            {
                UpdateTabVisuals();
                return;
            }

            _tabHeaderGO = new GameObject("TabHeader", typeof(RectTransform));
            _tabHeaderGO.transform.SetParent(transform, false);
            
            var rect = _tabHeaderGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.02f, 0.15f);
            rect.anchorMax = new Vector2(0.18f, 0.85f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var layout = _tabHeaderGO.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 15f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            _tabButtons.Clear();
            _tabTexts.Clear();

            for (int i = 0; i < 5; i++)
            {
                int tabIndex = i;
                string tabName = GetLocalizedTabName(tabIndex);

                var tabBtnGO = new GameObject($"Btn_Tab_{tabIndex}", typeof(RectTransform), typeof(Image), typeof(Button));
                tabBtnGO.transform.SetParent(_tabHeaderGO.transform, false);
                
                var btn = tabBtnGO.GetComponent<Button>();
                
                var textGO = new GameObject("Text", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                textGO.transform.SetParent(tabBtnGO.transform, false);
                var txt = textGO.GetComponent<TMPro.TextMeshProUGUI>();
                txt.text = tabName;
                txt.alignment = TMPro.TextAlignmentOptions.Center;
                txt.color = Color.black;
                txt.fontSize = 16f;

                btn.onClick.AddListener(() => SwitchTab(tabIndex));

                _tabButtons.Add(btn);
                _tabTexts.Add(txt);
            }

            UpdateTabVisuals();
        }

        private void SwitchTab(int tabIndex)
        {
            if (tabIndex == 4) // Tab Sở hữu (Kho Công Trình)
            {
                int mainHallLvl = GetMainHallLevel();
                if (mainHallLvl < 2)
                {
                    GameClient.UIManager.Instance.ShowMessage("Chưa Mở Khóa", "Yêu cầu Đại Điện (Main Hall) đạt Cấp 2 để sử dụng Kho công trình!");
                    return;
                }
            }

            _activeTab = tabIndex;
            UpdateTabVisuals();
            _ = PopulateBuildingListAsync();
        }

        private int GetMainHallLevel()
        {
            var mainHall = BaseBuildingManager.Instance.GetFirstBuilding("main_hall");
            if (mainHall != null) return mainHall.CurrentLevel;

            var dbMainHall = GameManager.Instance.PlayerBuildings.Find(b => b.BuildingCode == "main_hall");
            if (dbMainHall != null) return dbMainHall.Level;

            return 1;
        }

        private void UpdateTabVisuals()
        {
            int ownedUnplacedCount = GetOwnedUnplacedCount();

            // 1. Cập nhật cho trường hợp gán tay qua Inspector
            if (_btnTabProduction != null)
            {
                SetButtonColor(_btnTabProduction, _activeTab == 0);
                var txt = _btnTabProduction.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (txt != null) txt.text = GetLocalizedTabName(0);
            }
            if (_btnTabFacility != null)
            {
                SetButtonColor(_btnTabFacility, _activeTab == 1);
                var txt = _btnTabFacility.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (txt != null) txt.text = GetLocalizedTabName(1);
            }
            if (_btnTabShop != null)
            {
                SetButtonColor(_btnTabShop, _activeTab == 2);
                var txt = _btnTabShop.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (txt != null) txt.text = GetLocalizedTabName(2);
            }
            if (_btnTabScenery != null)
            {
                SetButtonColor(_btnTabScenery, _activeTab == 3);
                var txt = _btnTabScenery.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (txt != null) txt.text = GetLocalizedTabName(3);
            }
            if (_btnTabInventory != null)
            {
                SetButtonColor(_btnTabInventory, _activeTab == 4);
                var txt = _btnTabInventory.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (txt != null)
                {
                    txt.text = GetLocalizedTabName(4, ownedUnplacedCount);
                }
            }

            // 2. Cập nhật cho trường hợp tự sinh động
            if (_tabButtons.Count > 0)
            {
                for (int i = 0; i < _tabButtons.Count; i++)
                {
                    var img = _tabButtons[i].GetComponent<Image>();
                    var txt = _tabTexts[i];

                    if (txt != null)
                    {
                        txt.text = GetLocalizedTabName(i, i == 4 ? ownedUnplacedCount : 0);
                    }

                    if (i == _activeTab)
                    {
                        if (img != null) img.color = new Color(0.12f, 0.73f, 0.61f, 1f); // Active Teal
                        if (txt != null) txt.color = Color.white;
                    }
                    else
                    {
                        if (img != null) img.color = new Color(0.15f, 0.17f, 0.2f, 0.8f); // Inactive dark ash
                        if (txt != null) txt.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                    }
                }
            }
        }

        private void SetButtonColor(Button btn, bool isActive)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            var txt = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (img != null)
            {
                img.color = isActive ? new Color(0.12f, 0.73f, 0.61f, 1f) : new Color(0.15f, 0.17f, 0.2f, 0.8f);
            }
            if (txt != null)
            {
                txt.color = isActive ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
            }
        }

        private int GetOwnedUnplacedCount()
        {
            int ownedUnplacedCount = 0;
            var database = BaseBuildingManager.Instance.GetBuildingDatabase();
            foreach (var kvp in database)
            {
                var buildingData = kvp.Value;
                int ownedCount = 0;
                foreach (var b in GameManager.Instance.PlayerBuildings)
                {
                    if (b.BuildingCode == buildingData.BuildingID) ownedCount++;
                }
                int placedCount = BaseBuildingManager.Instance.GetBuildingCount(buildingData.BuildingID);
                if (ownedCount > placedCount)
                {
                    ownedUnplacedCount += (ownedCount - placedCount);
                }
            }
            return ownedUnplacedCount;
        }

        private async Task PopulateBuildingListAsync()
        {
            // Tải kho tài nguyên một lần cho toàn bộ list
            GameClient.Network.Pb.Inventory inventory = null;
            try
            {
                inventory = await GameClient.Network.Api.SectBuildingApi.GetInventoryAsync();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[BuildMenuPanel] Lỗi tải tài nguyên từ kho: {ex.Message}");
            }

            foreach (Transform child in _buildingListContainer)
            {
                Destroy(child.gameObject);
            }

            var database = BaseBuildingManager.Instance.GetBuildingDatabase();
            foreach (var kvp in database)
            {
                var buildingData = kvp.Value;
                
                int ownedCount = 0;
                foreach (var b in GameManager.Instance.PlayerBuildings)
                {
                    if (b.BuildingCode == buildingData.BuildingID) ownedCount++;
                }
                int placedCount = BaseBuildingManager.Instance.GetBuildingCount(buildingData.BuildingID);
                bool isOwned = ownedCount > 0;
                bool hasUnplaced = ownedCount > placedCount;
                var dbBuilding = GameManager.Instance.PlayerBuildings.Find(b => 
                    b.BuildingCode == buildingData.BuildingID && 
                    !BaseBuildingManager.Instance.IsBuildingPlaced(b.InstanceId));

                // Quy tắc lọc danh sách theo Tab:
                if (_activeTab == 4) // Tab Sở hữu (Inventory)
                {
                    // Chỉ hiện các công trình đã sở hữu (có trong DB) nhưng chưa được đặt hết trên map
                    if (!isOwned) continue;
                    if (!hasUnplaced) continue;
                }
                else // Các Tab Xây Mới chia theo Category: Sản xuất (0), Cơ sở (1), Cửa Hiệu (2), Phong Cảnh (3)
                {
                    // Chỉ hiển thị công trình có Category khớp với Tab được chọn
                    if ((int)buildingData.Category != _activeTab) continue;
                }

                GameObject itemGo = null;
                if (!string.IsNullOrEmpty(_buildingItemPrefabAddress))
                {
                    itemGo = await ResourceManager.Instance.InstantiateAsync(_buildingItemPrefabAddress, _buildingListContainer);
                }

                if (itemGo == null)
                {
                    Debug.LogWarning("[BuildMenuPanel] Không thể load building item prefab!");
                    continue;
                }

                itemGo.SetActive(true);
                SetupBuildingButton(itemGo, buildingData, inventory);
            }
        }

        private bool CheckBuildRequirements(BuildingData buildingData, GameClient.Network.Pb.Inventory inventory, out string missingInfo)
        {
            missingInfo = "";
            bool isSatisfied = true;
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            int reqRep = (buildingData.LevelStats != null && buildingData.LevelStats.Count > 0) ? buildingData.LevelStats[0].RequiredReputation : 0;
            if (GameContext.SectReputation < reqRep)
            {
                isSatisfied = false;
                sb.Append($"Uy Danh {GameContext.SectReputation}/{reqRep} ");
            }

            var costItems = (buildingData.LevelStats != null && buildingData.LevelStats.Count > 0) ? buildingData.LevelStats[0].CostItems : null;
            if (costItems != null && inventory != null && inventory.Items != null)
            {
                foreach (var cost in costItems)
                {
                    if (string.IsNullOrEmpty(cost.ItemCode)) continue;
                    int ownedQty = 0;
                    foreach (var item in inventory.Items)
                    {
                        if (item.ItemCode == cost.ItemCode)
                        {
                            ownedQty = (int)item.Quantity;
                            break;
                        }
                    }

                    if (ownedQty < cost.Quantity)
                    {
                        isSatisfied = false;
                        var config = ItemDataManager.Instance.GetItemConfig(cost.ItemCode);
                        string itemName = "";
                        if (config != null && !string.IsNullOrEmpty(config.NameKey))
                        {
                            itemName = config.NameKey;
                        }
                        else
                        {
                            if (cost.ItemCode == "00000" || cost.ItemCode == "coin") itemName = "coin";
                            else if (cost.ItemCode == "00002" || cost.ItemCode == "stone" || cost.ItemCode == "stone_1") itemName = "stone_1";
                            else if (cost.ItemCode == "00003" || cost.ItemCode == "wood" || cost.ItemCode == "wood_1") itemName = "wood_1";
                            else if (cost.ItemCode == "gold") itemName = "gold";
                            else itemName = cost.ItemCode;
                        }
                        
                        // Lấy ngôn ngữ dịch nếu có
                        string localizedItemName = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.ITEM_EQUIPMENT, itemName);
                        if (string.IsNullOrEmpty(localizedItemName) || localizedItemName.StartsWith("[")) localizedItemName = itemName;

                        sb.Append($"Thiếu {localizedItemName} ({ownedQty}/{cost.Quantity}) ");
                    }
                }
            }

            missingInfo = sb.ToString().Trim();
            return isSatisfied;
        }

        private void SetupBuildingButton(GameObject btnGo, BuildingData buildingData, GameClient.Network.Pb.Inventory inventory)
        {
            var item = btnGo.GetComponent<BuildItem>();
            
            var dbBuilding = GameManager.Instance.PlayerBuildings.Find(b => 
                b.BuildingCode == buildingData.BuildingID && 
                !BaseBuildingManager.Instance.IsBuildingPlaced(b.InstanceId));
            bool isOwned = dbBuilding != null;

            // Tính toán giới hạn số lượng công trình
            int placedCount = BaseBuildingManager.Instance.GetBuildingCount(buildingData.BuildingID);
            int maxAllowed = buildingData.GetMaxLimit(GameContext.SectReputation);
            string limitStr = maxAllowed >= 0 ? $"{placedCount}/{maxAllowed}" : "";

            if (item != null)
            {
                // Sử dụng kéo thả từ BuildItem component
                if (item.imgPreview != null && buildingData.VisualConfig != null)
                {
                    var visual = buildingData.VisualConfig.GetVisualsForLevel(1);
                    if (visual != null && visual.normalSprite != null)
                    {
                        item.imgPreview.sprite = visual.normalSprite;
                        item.imgPreview.preserveAspect = true;
                    }
                }

                if (item.txtName != null)
                {
                    string statusText = "";
                    if (_activeTab == 4 && isOwned)
                    {
                        statusText = $" (Cấp {dbBuilding.Level} - Sẵn sàng)";
                    }
                    
                    string localizedName = LocalizationManager.Instance.GetText(buildingData.BuildingNameKey);
                    if (string.IsNullOrEmpty(localizedName) || localizedName.StartsWith("[")) localizedName = buildingData.BuildingNameKey;

                    var localizeEvent = item.txtName.GetComponent<UnityEngine.Localization.Components.LocalizeStringEvent>();
                    if (localizeEvent != null)
                    {
                        localizeEvent.enabled = false;
                    }

                    item.txtName.text = localizedName + statusText;
                }

                if (maxAllowed >= 0)
                {
                    if (item.limitBg != null) item.limitBg.SetActive(true);
                    if (item.txtLimit != null) item.txtLimit.text = limitStr;
                }
                else
                {
                    if (item.limitBg != null) item.limitBg.SetActive(false);
                    else if (item.txtLimit != null) item.txtLimit.text = "";
                }

                if (item.btnAction != null)
                {
                    item.btnAction.onClick.RemoveAllListeners();

                    if (maxAllowed >= 0 && placedCount >= maxAllowed)
                    {
                        if (item.txtBtnStatus != null) item.txtBtnStatus.text = "Đã xây xong";
                        item.btnAction.interactable = false;
                    }
                    else
                    {
                        string missingInfo;
                        bool canBuild = CheckBuildRequirements(buildingData, inventory, out missingInfo);

                        if (canBuild)
                        {
                            if (item.txtBtnStatus != null) item.txtBtnStatus.text = "Xây Dựng";
                            item.btnAction.interactable = true;
                            item.btnAction.onClick.AddListener(() => OnBuildingButtonClicked(buildingData));
                        }
                        else
                        {
                            if (item.txtBtnStatus != null) item.txtBtnStatus.text = missingInfo;
                            item.btnAction.interactable = false;
                        }
                    }
                }
            }
            else
            {
                // Fallback cũ nếu không gắn script BuildItem
                var txtName = btnGo.transform.Find("Txt_Name")?.GetComponent<TMPro.TextMeshProUGUI>();
                var txtLimit = btnGo.transform.Find("Image/Txt_Limit")?.GetComponent<TMPro.TextMeshProUGUI>();
                var limitBg = btnGo.transform.Find("Image")?.gameObject;
                var btn = btnGo.transform.Find("Btn_Action")?.GetComponent<Button>();
                var btnText = btnGo.transform.Find("Btn_Action/Text (TMP)")?.GetComponent<TMPro.TextMeshProUGUI>();

                // Fallback phòng hờ cấu trúc thay đổi
                var fallbackTexts = btnGo.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
                if (txtName == null && fallbackTexts.Length > 0) txtName = fallbackTexts[0];
                if (txtLimit == null && fallbackTexts.Length > 1) txtLimit = fallbackTexts[1];
                if (btn == null) btn = btnGo.GetComponentInChildren<Button>();
                if (btnText == null && btn != null) btnText = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();

                // Gán tên công trình
                if (txtName != null)
                {
                    string statusText = "";
                    if (_activeTab == 4 && isOwned)
                    {
                        statusText = $" (Cấp {dbBuilding.Level} - Sẵn sàng)";
                    }
                    
                    // Lấy ngôn ngữ dịch cho tên công trình nếu có
                    string localizedName = LocalizationManager.Instance.GetText(buildingData.BuildingNameKey);
                    if (string.IsNullOrEmpty(localizedName) || localizedName.StartsWith("[")) localizedName = buildingData.BuildingNameKey;

                    txtName.text = localizedName + statusText;
                }

                // Gán giới hạn & Ẩn hiện khung Limit tùy theo cấu hình
                if (maxAllowed >= 0)
                {
                    if (limitBg != null) limitBg.SetActive(true);
                    if (txtLimit != null) txtLimit.text = limitStr;
                }
                else
                {
                    // Không cấu hình giới hạn -> Ẩn luôn khung nền đen limit
                    if (limitBg != null) limitBg.SetActive(false);
                    else if (txtLimit != null) txtLimit.text = "";
                }

                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();

                    if (maxAllowed >= 0 && placedCount >= maxAllowed)
                    {
                        // Trường hợp 1: Đã xây tối đa
                        if (btnText != null) btnText.text = "Đã xây xong";
                        btn.interactable = false;
                    }
                    else
                    {
                        // Trường hợp 2: Kiểm tra nguyên liệu và uy danh yêu cầu
                        string missingInfo;
                        bool canBuild = CheckBuildRequirements(buildingData, inventory, out missingInfo);

                        if (canBuild)
                        {
                            if (btnText != null) btnText.text = "Xây Dựng";
                            btn.interactable = true;
                            btn.onClick.AddListener(() => OnBuildingButtonClicked(buildingData));
                        }
                        else
                        {
                            if (btnText != null) btnText.text = missingInfo;
                            btn.interactable = false;
                        }
                    }
                }
            }
        }

        private void OnBuildingButtonClicked(BuildingData data)
        {
            Debug.Log($"[BuildMenuPanel] Đã chọn đặt nhà từ tab {_activeTab}: {data.BuildingID}");
            Hide(); // Ẩn Menu sau khi chọn xong

            var controller = FindFirstObjectByType<GameClient.BaseBuilding.Core.BuildingController>();
            if (controller != null)
            {
                // Luôn kiểm tra xem có công trình nào cùng loại trong kho (chưa đặt lên map) hay không
                var unplaced = GameManager.Instance.PlayerBuildings.Find(b => 
                    b.BuildingCode == data.BuildingID && 
                    !BaseBuildingManager.Instance.IsBuildingPlaced(b.InstanceId));

                long instanceId = 0;
                int level = 1;

                if (unplaced != null)
                {
                    instanceId = unplaced.InstanceId;
                    level = unplaced.Level;
                    Debug.Log($"[BuildMenuPanel] Phát hiện công trình {data.BuildingID} trong kho (Cấp {level}, ID {instanceId}). Sử dụng lại công trình này!");
                }

                controller.StartPlacement(data.BuildingID, data.PrefabAddress, data.SizeX, data.SizeY, instanceId, level);
            }
            else
            {
                Debug.LogError("[BuildMenuPanel] Không tìm thấy BuildingController trong Scene!");
            }
        }
    }
}
