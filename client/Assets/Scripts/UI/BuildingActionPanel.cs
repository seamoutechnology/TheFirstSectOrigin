using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameClient.UI.Core;
using GameClient.Gameplay.BaseBuilder;
using GameClient.BaseBuilding.Core;
using GameClient.Network.Api;
using GameClient;
using GameClient.Managers;
using System.Threading.Tasks;

namespace GameClient.UI
{
    public class BuildingActionPanel : BaseUIPanel
    {
        [System.Serializable]
        public struct ActionConfig
        {
            public string localizationKey;
            public Sprite sprite;
        }

        [Header("UI Elements")]
        [SerializeField] private TMP_Text txtBuildingName;
        [SerializeField] private TMP_Text txtBuildingLevel;
        [SerializeField] private TMP_Text txtBuildingStatus;

        [Header("Dynamic Button Settings")]
        [SerializeField] private GameObject actionButtonPrefab;
        [SerializeField] private ActionConfig infoConfig;
        [SerializeField] private ActionConfig upgradeConfig;
        [SerializeField] private ActionConfig harvestConfig;
        [SerializeField] private ActionConfig speedUpConfig;
        [SerializeField] private ActionConfig repairConfig;
        [SerializeField] private ActionConfig cancelConfig;
        [SerializeField] private ActionConfig moveConfig;
        [SerializeField] private ActionConfig demolishConfig;
        [SerializeField] private ActionConfig closeConfig;
        [SerializeField] private ActionConfig confirmPlacementConfig;
        [SerializeField] private ActionConfig cancelPlacementConfig;

        [Header("Radial Layout Settings")]
        [SerializeField] private bool useRadialLayout = true;
        [SerializeField] private float radialRadius = 120f;
        [SerializeField] private float startAngleDegrees = 90f; // Bắt đầu từ phía trên (90 độ)
        [SerializeField] private float panelScale = 1.4f; // Tăng tỷ lệ hiển thị của bảng lên 1.4 lần cho đỡ bé

        private BuildingInstance _currentBuilding;
        private BuildingController _currentController;
        private bool _isPlacementMode;
        private readonly System.Collections.Generic.List<GameObject> _spawnedButtons = new();
        private GameObject _closeButtonGo;

        protected override void Awake()
        {
            base.Awake();
        }

        public override void Setup(object data)
        {
            base.Setup(data);
            
            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                rect.localScale = new Vector3(panelScale, panelScale, 1f);
            }

            if (data is BuildingInstance building)
            {
                _currentBuilding = building;
                _currentController = null;
                _isPlacementMode = false;
                PositionPanelAtBuilding();
                RefreshUI();
            }
            else if (data is BuildingController controller)
            {
                _currentBuilding = null;
                _currentController = controller;
                _isPlacementMode = true;
                PositionPanelAtPreview();
                RefreshUI();
            }
            else
            {
                Debug.LogError("[BuildingActionPanel] Data truyền vào không hợp lệ!");
                Hide();
            }
        }

        private void Update()
        {
            if (gameObject.activeInHierarchy)
            {
                if (_isPlacementMode && _currentController != null)
                {
                    PositionPanelAtPreview();
                }
                else if (_currentBuilding != null)
                {
                    PositionPanelAtBuilding();
                }
            }
        }

        private void PositionPanelAtBuilding()
        {
            if (_currentBuilding == null || Camera.main == null) return;

            Vector3 worldPos = _currentBuilding.transform.position;
            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                rect.position = screenPos;
            }
        }

        private void PositionPanelAtPreview()
        {
            if (_currentController == null || _currentController.previewRenderer == null || Camera.main == null) return;

            Vector3 worldPos = _currentController.previewRenderer.transform.position;
            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            RectTransform rect = transform as RectTransform;
            if (rect != null)
            {
                rect.position = screenPos;
            }
        }

        private void ClearSpawnedButtons()
        {
            foreach (var btn in _spawnedButtons)
            {
                if (btn != null) Destroy(btn);
            }
            _spawnedButtons.Clear();
            _closeButtonGo = null;
        }

        private Button CreateButton(ActionConfig config, System.Action onClickAction)
        {
            if (actionButtonPrefab == null) return null;

            var go = Instantiate(actionButtonPrefab, transform);
            var btn = go.GetComponent<Button>();
            if (btn == null) btn = go.AddComponent<Button>();

            // Set Sprite for the Icon child
            var iconTransform = go.transform.Find("Icon");
            if (iconTransform != null)
            {
                var img = iconTransform.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = config.sprite;
                }
            }

            // Set localized text
            var textTransform = go.transform.Find("Text");
            if (textTransform != null)
            {
                var txt = textTransform.GetComponent<TMP_Text>();
                if (txt != null)
                {
                    string text = LocalizationManager.Instance.GetText(config.localizationKey);
                    if (string.IsNullOrEmpty(text) || text.StartsWith("["))
                    {
                        text = GetFallbackText(config.localizationKey);
                    }
                    txt.text = text;
                }
            }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClickAction?.Invoke());

            _spawnedButtons.Add(go);
            return btn;
        }

        private string GetFallbackText(string key)
        {
            switch (key)
            {
                case "ui_label_building_action_info": return "Thông Tin";
                case "ui_label_building_action_upgrade": return "Nâng Cấp";
                case "ui_label_building_action_harvest": return "Thu Hoạch";
                case "ui_label_building_action_speedup": return "Tăng Tốc";
                case "ui_label_building_action_repair": return "Sửa Chữa";
                case "ui_label_building_action_cancel": return "Hủy";
                case "ui_label_building_action_move": return "Di Chuyển";
                case "ui_label_building_action_demolish": return "Cất Kho";
                case "ui_label_building_action_close": return "Đóng";
                case "ui_label_building_action_placement_confirm": return "OK";
                case "ui_label_building_action_placement_cancel": return "✗ Hủy";
                default: return "";
            }
        }

        private void OnConfirmPlacementClicked()
        {
            if (_currentController != null)
            {
                _currentController.TryPlaceBuilding();
            }
        }

        private void OnCancelPlacementClicked()
        {
            if (_currentController != null)
            {
                _currentController.CancelPlacement();
            }
        }

        private void RefreshUI()
        {
            ClearSpawnedButtons();

            if (_isPlacementMode)
            {
                var confirmBtn = CreateButton(confirmPlacementConfig, OnConfirmPlacementClicked);
                if (confirmBtn != null)
                {
                    var img = confirmBtn.GetComponent<Image>();
                    if (img != null && confirmPlacementConfig.sprite == null) img.color = new Color(0.2f, 0.8f, 0.2f, 1f);
                }

                var cancelBtn = CreateButton(cancelPlacementConfig, OnCancelPlacementClicked);
                if (cancelBtn != null)
                {
                    var img = cancelBtn.GetComponent<Image>();
                    if (img != null && cancelPlacementConfig.sprite == null) img.color = new Color(0.8f, 0.2f, 0.2f, 1f);
                }

                if (_spawnedButtons.Count >= 2)
                {
                    var rectConfirm = _spawnedButtons[0].transform as RectTransform;
                    var rectCancel = _spawnedButtons[1].transform as RectTransform;
                    if (rectConfirm != null) rectConfirm.anchoredPosition = new Vector2(65, -85);
                    if (rectCancel != null) rectCancel.anchoredPosition = new Vector2(-65, -85);
                }

                string choosingPosText = LocalizationManager.Instance.GetText("ui_building_action_choosing_position");
                if (string.IsNullOrEmpty(choosingPosText) || choosingPosText.StartsWith("[")) choosingPosText = "Đang Chọn Vị Trí";

                string moveInstructionText = LocalizationManager.Instance.GetText("ui_building_action_move_instruction");
                if (string.IsNullOrEmpty(moveInstructionText) || moveInstructionText.StartsWith("[")) moveInstructionText = "Di chuyển đến ô đất trống...";

                if (txtBuildingName != null) txtBuildingName.text = choosingPosText;
                if (txtBuildingLevel != null) txtBuildingLevel.text = "";
                if (txtBuildingStatus != null) txtBuildingStatus.text = moveInstructionText;
                return;
            }

            if (_currentBuilding == null || _currentBuilding.Data == null) return;

            string localizedName = LocalizationManager.Instance.GetText(_currentBuilding.Data.BuildingNameKey);
            if (string.IsNullOrEmpty(localizedName) || localizedName.StartsWith("[")) localizedName = _currentBuilding.Data.BuildingNameKey;

            if (txtBuildingName != null) txtBuildingName.text = localizedName;
            if (txtBuildingLevel != null) txtBuildingLevel.text = $"Lv.{_currentBuilding.CurrentLevel}";
            
            string statusStr = GetStatusString(_currentBuilding.CurrentState);
            if (txtBuildingStatus != null) txtBuildingStatus.text = statusStr;

            var state = _currentBuilding.CurrentState;
            bool isProducer = _currentBuilding.Data is ProductionBuildingData;
            bool canHarvest = _currentBuilding.HasResourcesToHarvest();

            // 1. Info Button
            CreateButton(infoConfig, OnInfoClicked);

            // 2. Upgrade Button
            if (state == BuildingState.Normal || state == BuildingState.Producing || state == BuildingState.ReadyToHarvest)
            {
                var upBtn = CreateButton(upgradeConfig, OnUpgradeClicked);
                if (upBtn != null)
                {
                    if (_currentBuilding.IsMaxLevel())
                    {
                        upBtn.interactable = false;
                        var txt = upBtn.GetComponentInChildren<TMP_Text>();
                        if (txt != null) txt.text = LocalizationManager.Instance.GetText("ui_building_max_level") ?? "Cực Hạn";
                    }
                }
            }

            // 3. Harvest Button
            if (isProducer && (state == BuildingState.Producing || state == BuildingState.ReadyToHarvest))
            {
                var harvestBtn = CreateButton(harvestConfig, OnHarvestClicked);
                if (harvestBtn != null)
                {
                    harvestBtn.interactable = canHarvest;
                }
            }

            // 4. SpeedUp Button
            if (state == BuildingState.Building || state == BuildingState.Upgrading)
            {
                CreateButton(speedUpConfig, OnSpeedUpClicked);
            }

            // 5. Repair Button
            if (state == BuildingState.Broken)
            {
                CreateButton(repairConfig, OnRepairClicked);
            }

            // 6. Cancel Button
            if (state == BuildingState.Building || state == BuildingState.Upgrading)
            {
                CreateButton(cancelConfig, OnCancelClicked);
            }

            // 7. Move Button
            if (state == BuildingState.Normal || state == BuildingState.Producing || state == BuildingState.ReadyToHarvest)
            {
                CreateButton(moveConfig, OnMoveClicked);
            }

            // 8. Demolish Button
            if (state == BuildingState.Normal || state == BuildingState.Broken || 
                state == BuildingState.Producing || state == BuildingState.ReadyToHarvest)
            {
                CreateButton(demolishConfig, OnDemolishClicked);
            }

            // 9. Close Button
            var closeBtn = CreateButton(closeConfig, Hide);
            if (closeBtn != null)
            {
                _closeButtonGo = closeBtn.gameObject;
            }

            if (useRadialLayout)
            {
                ApplyRadialLayout();
            }
        }

        private void ApplyRadialLayout()
        {
            var radialButtons = new System.Collections.Generic.List<RectTransform>();
            foreach (var go in _spawnedButtons)
            {
                if (go == null) continue;
                if (go == _closeButtonGo)
                {
                    var closeRect = go.transform as RectTransform;
                    if (closeRect != null)
                    {
                        closeRect.anchoredPosition = Vector2.zero; // Nút đóng ở giữa tâm
                    }
                    continue;
                }
                radialButtons.Add(go.transform as RectTransform);
            }

            int count = radialButtons.Count;
            if (count == 0) return;

            if (count == 1)
            {
                float angleRad = 270f * Mathf.Deg2Rad; // Nằm thẳng đứng bên dưới
                float x = Mathf.Cos(angleRad) * radialRadius;
                float y = Mathf.Sin(angleRad) * radialRadius;
                radialButtons[0].anchoredPosition = new Vector2(x, y);
            }
            else
            {
                // Phân bổ đều các nút trong cung 180 độ bên dưới (từ 180 độ bên trái sang 360 độ bên phải)
                float startAngle = 180f;
                float sweepAngle = 180f;
                float angleStep = sweepAngle / (count - 1);

                for (int i = 0; i < count; i++)
                {
                    float angleDeg = startAngle + i * angleStep;
                    float angleRad = angleDeg * Mathf.Deg2Rad;

                    float x = Mathf.Cos(angleRad) * radialRadius;
                    float y = Mathf.Sin(angleRad) * radialRadius;

                    radialButtons[i].anchoredPosition = new Vector2(x, y);
                }
            }
        }

        private string GetStatusString(BuildingState state)
        {
            switch (state)
            {
                case BuildingState.Normal: return "Bình Thường";
                case BuildingState.Ghost: return "Bản Vẽ";
                case BuildingState.Building: return "Đang Xây Dựng...";
                case BuildingState.Upgrading: return "Đang Nâng Cấp...";
                case BuildingState.Broken: return "Bị Hỏng / Cần Sửa Chữa";
                case BuildingState.Locked: return "Chưa Mở Khóa";
                case BuildingState.Producing: return "Đang Sản Xuất";
                case BuildingState.ReadyToHarvest: return "Đã Chín / Sẵn Sàng";
                default: return state.ToString();
            }
        }

        private void OnInfoClicked()
        {
            if (GameClient.UIManager.Instance != null)
            {
                GameClient.UIManager.Instance.OpenPanel("UI_BuildingDetailPanel", _currentBuilding);
                Hide();
            }
        }

        private async void OnUpgradeClicked()
        {
            if (_currentBuilding == null) return;
            
            try
            {
                long instanceId = _currentBuilding.InstanceID;
                if (instanceId == 0)
                {
                    GameClient.UIManager.Instance.ShowMessage("Lỗi", "Tòa nhà chưa được đồng bộ với Server!");
                    return;
                }

                var resp = await SectBuildingApi.UpgradeBuildingAsync(instanceId);
                
                if (resp != null && resp.Base != null && resp.Base.Code == 0)
                {
                    string buildingName = GameClient.Managers.LocalizationManager.Instance.GetText(_currentBuilding.Data.BuildingNameKey); 
                    if (string.IsNullOrEmpty(buildingName) || buildingName.StartsWith("[")) { buildingName = _currentBuilding.Data.BuildingNameKey; } 
                    int targetLevel = _currentBuilding.CurrentLevel + 1; 
                    string successTitle = GameClient.Managers.LocalizationManager.Instance.GetText("UI_System", "ui_success_title") ?? "Thành Công"; 
                    string successFormat = GameClient.Managers.LocalizationManager.Instance.GetText("UI_System", "ui_building_upgrade_start_success") ?? "Đã bắt đầu nâng cấp toà nhà {0} lên Cấp {1}!"; 
                    GameClient.UIManager.Instance.ShowMessage(successTitle, string.Format(successFormat, buildingName, targetLevel));
                    
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
                    errorMsg = LocalizeUpgradeError(errorMsg);
                    GameClient.UIManager.Instance.ShowMessage("Lỗi Nâng Cấp", errorMsg);
                }
            }
            catch (System.Exception ex)
            {
                GameClient.UIManager.Instance.ShowMessage("Lỗi Nâng Cấp", ex.Message);
            }
        }

        private string LocalizeUpgradeError(string errorMsg)
        {
            if (string.IsNullOrEmpty(errorMsg)) return errorMsg;

            string[] lines = errorMsg.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string trimmed = lines[i].Trim();
                if (trimmed.StartsWith("- "))
                {
                    string rawLine = trimmed.Substring(2);
                    int spaceIndex = rawLine.IndexOf(' ');
                    if (spaceIndex > 0)
                    {
                        string itemCode = rawLine.Substring(0, spaceIndex);
                        string remainder = rawLine.Substring(spaceIndex);

                        string localizedName = GameClient.Managers.LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.ITEM_EQUIPMENT, itemCode);
                        if (!string.IsNullOrEmpty(localizedName) && !localizedName.StartsWith("["))
                        {
                            lines[i] = $"- {localizedName}{remainder}";
                        }
                    }
                }
            }
            return string.Join("\n", lines);
        }


        private async void OnHarvestClicked()
        {
            if (_currentBuilding == null || !_currentBuilding.HasResourcesToHarvest()) return;
            
            try
            {
                long instanceId = _currentBuilding.InstanceID;
                var resp = await SectBuildingApi.CollectResourcesAsync(instanceId);
                
                if (resp != null)
                {
                    string message = "";
                    if (resp.Resources != null && resp.Resources.Count > 0)
                    {
                        var itemsList = new System.Collections.Generic.List<string>();
                        foreach (var kvp in resp.Resources)
                        {
                            string name = kvp.Key;
                            if (name == "herb_spirit") name = "Linh Thảo";
                            else if (name == "iron_ore") name = "Quặng Sắt";
                            else if (name == "00003") name = "Gỗ I";
                            else if (name == "00002") name = "Đá I";
                            itemsList.Add($"+{kvp.Value} {name}");
                        }
                        message = "Nhận được: " + string.Join(", ", itemsList);
                    }
                    else
                    {
                        message = $"Nhận được {resp.GoldGained} Vàng!";
                    }
                    GameClient.UIManager.Instance.ShowMessage("Thu Hoạch Thành Công", message);
                    
                    var baseResp = await SectBuildingApi.GetBaseAsync();
                    if (baseResp != null)
                    {
                        GameClient.GameManager.Instance.SetBuildings(baseResp.Buildings);
                    }
                    Hide();
                }
            }
            catch (System.Exception ex)
            {
                GameClient.UIManager.Instance.ShowMessage("Lỗi Thu Hoạch", ex.Message);
            }
        }

        private async void OnSpeedUpClicked()
        {
            if (_currentBuilding == null) return;

            try
            {
                long instanceId = _currentBuilding.InstanceID;
                if (instanceId == 0) return;

                var resp = await SectBuildingApi.SpeedUpBuildingAsync(instanceId);
                if (resp != null && resp.Base != null && resp.Base.Code == 0)
                {
                    GameClient.UIManager.Instance.ShowMessage("Tăng Tốc Thành Công", "Đã tăng tốc nâng cấp toà nhà thành công!");
                    
                    var baseResp = await SectBuildingApi.GetBaseAsync();
                    if (baseResp != null && baseResp.Base != null && baseResp.Base.Code == 0)
                    {
                        GameClient.GameManager.Instance.SetBuildings(baseResp.Buildings);
                        BaseBuildingManager.Instance.SyncBuildingsWithServerData(baseResp.Buildings);
                    }
                    if (resp.Player != null)
                    {
                        GameClient.GameManager.Instance.SetPlayer(resp.Player);
                    }
                    Hide();
                }
                else
                {
                    string errorMsg = resp?.Base?.Message ?? "Lỗi không xác định từ Server";
                    errorMsg = LocalizeUpgradeError(errorMsg);
                    GameClient.UIManager.Instance.ShowMessage("Lỗi Tăng Tốc", errorMsg);
                }
            }
            catch (System.Exception ex)
            {
                GameClient.UIManager.Instance.ShowMessage("Lỗi Tăng Tốc", ex.Message);
            }
        }

        private void OnRepairClicked()
        {
            GameClient.UIManager.Instance.ShowMessage("Sửa Chữa", $"Bắt đầu sửa chữa {_currentBuilding.Data.BuildingNameKey}!");
            _currentBuilding.SetState(BuildingState.Normal);
            RefreshUI();
        }

        private void OnCancelClicked()
        {
            GameClient.UIManager.Instance.ShowMessage("Hủy Tiến Trình", $"Đã hủy tiến trình của {_currentBuilding.Data.BuildingNameKey}!");
            _currentBuilding.SetState(BuildingState.Normal);
            RefreshUI();
        }

        private void OnMoveClicked()
        {
            var controller = FindFirstObjectByType<BuildingController>();
            if (controller != null)
            {
                BaseGridManager.Instance.SetOccupied(_currentBuilding.GridX, _currentBuilding.GridY, _currentBuilding.Data.SizeX, _currentBuilding.Data.SizeY, false);
                controller.StartMove(_currentBuilding);
            }
            else
            {
                GameClient.UIManager.Instance.ShowMessage("Lỗi", "Không tìm thấy BuildingController để di chuyển!");
            }
        }

        private void OnDemolishClicked()
        {
            string buildingName = LocalizationManager.Instance.GetText(_currentBuilding.Data.BuildingNameKey);
            if (string.IsNullOrEmpty(buildingName) || buildingName.StartsWith("[")) buildingName = _currentBuilding.Data.BuildingNameKey;

            GameClient.UIManager.Instance.ShowConfirmDialog(
                "ui_stow_title", 
                "ui_stow_confirm", 
                buildingName,
                "ui_label_accept", 
                "ui_label_deny",
                async () => {
                    BaseBuildingManager.Instance.RemoveBuilding(_currentBuilding);
                    await BaseBuildingManager.Instance.SaveLayoutToServer();
                    Hide();
                }
            );
        }
    }
}
