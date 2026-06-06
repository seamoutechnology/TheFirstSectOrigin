using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameClient.UI.Core;
using GameClient.Gameplay.BaseBuilder;
using GameClient.BaseBuilding.Core;
using GameClient.Network.Api;
using GameClient;
using System.Threading.Tasks;

namespace GameClient.UI
{
    public class BuildingActionPanel : BaseUIPanel
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text txtBuildingName;
        [SerializeField] private TMP_Text txtBuildingLevel;
        [SerializeField] private TMP_Text txtBuildingStatus;
        
        [Header("Buttons")]
        [SerializeField] private Button btnInfo;
        [SerializeField] private Button btnUpgrade;
        [SerializeField] private Button btnHarvest;
        [SerializeField] private Button btnSpeedUp;
        [SerializeField] private Button btnRepair;
        [SerializeField] private Button btnCancel;
        [SerializeField] private Button btnMove;
        [SerializeField] private Button btnDemolish;
        [SerializeField] private Button btnClose;

        [Header("Radial Layout Settings")]
        [SerializeField] private bool useRadialLayout = true;
        [SerializeField] private float radialRadius = 120f;
        [SerializeField] private float startAngleDegrees = 90f; // Bắt đầu từ phía trên (90 độ)
        [SerializeField] private float panelScale = 1.4f; // Tăng tỷ lệ hiển thị của bảng lên 1.4 lần cho đỡ bé

        private BuildingInstance _currentBuilding;

        protected override void Awake()
        {
            base.Awake();
            
            if (btnInfo != null) btnInfo.onClick.AddListener(OnInfoClicked);
            if (btnUpgrade != null) btnUpgrade.onClick.AddListener(OnUpgradeClicked);
            if (btnHarvest != null) btnHarvest.onClick.AddListener(OnHarvestClicked);
            if (btnSpeedUp != null) btnSpeedUp.onClick.AddListener(OnSpeedUpClicked);
            if (btnRepair != null) btnRepair.onClick.AddListener(OnRepairClicked);
            if (btnCancel != null) btnCancel.onClick.AddListener(OnCancelClicked);
            if (btnMove != null) btnMove.onClick.AddListener(OnMoveClicked);
            if (btnDemolish != null) btnDemolish.onClick.AddListener(OnDemolishClicked);
            if (btnClose != null) btnClose.onClick.AddListener(Hide);
        }

        private Button _btnConfirmPlacement;
        private Button _btnCancelPlacement;
        private BuildingController _currentController;
        private bool _isPlacementMode;

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

        private void SetupPlacementButtons()
        {
            if (_btnConfirmPlacement != null) return;

            Button templateBtn = btnUpgrade != null ? btnUpgrade : (btnInfo != null ? btnInfo : btnClose);
            if (templateBtn == null) return;

            // Confirm Button
            var confirmGo = Instantiate(templateBtn.gameObject, transform);
            confirmGo.name = "Btn_ConfirmPlacement";
            _btnConfirmPlacement = confirmGo.GetComponent<Button>();
            _btnConfirmPlacement.onClick.RemoveAllListeners();
            _btnConfirmPlacement.onClick.AddListener(OnConfirmPlacementClicked);
            confirmGo.SetActive(true); // Đảm bảo active
            
            var txt = confirmGo.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = "✓ OK";
            var img = confirmGo.GetComponent<Image>();
            if (img != null) img.color = new Color(0.2f, 0.8f, 0.2f, 1f); // Xanh lục tươi

            // Cancel Button
            var cancelGo = Instantiate(templateBtn.gameObject, transform);
            cancelGo.name = "Btn_CancelPlacement";
            _btnCancelPlacement = cancelGo.GetComponent<Button>();
            _btnCancelPlacement.onClick.RemoveAllListeners();
            _btnCancelPlacement.onClick.AddListener(OnCancelPlacementClicked);
            cancelGo.SetActive(true); // Đảm bảo active

            var txtCancel = cancelGo.GetComponentInChildren<TMP_Text>();
            if (txtCancel != null) txtCancel.text = "✗ Hủy";
            var imgCancel = cancelGo.GetComponent<Image>();
            if (imgCancel != null) imgCancel.color = new Color(0.8f, 0.2f, 0.2f, 1f); // Đỏ tươi
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
            if (_isPlacementMode)
            {
                // Ẩn toàn bộ nút thường
                Button[] normalButtons = { btnInfo, btnUpgrade, btnHarvest, btnSpeedUp, btnRepair, btnCancel, btnMove, btnDemolish, btnClose };
                foreach (var btn in normalButtons)
                {
                    if (btn != null) btn.gameObject.SetActive(false);
                }

                SetupPlacementButtons();

                if (_btnConfirmPlacement != null)
                {
                    _btnConfirmPlacement.gameObject.SetActive(true);
                    (_btnConfirmPlacement.transform as RectTransform).anchoredPosition = new Vector2(50, -85);
                }
                if (_btnCancelPlacement != null)
                {
                    _btnCancelPlacement.gameObject.SetActive(true);
                    (_btnCancelPlacement.transform as RectTransform).anchoredPosition = new Vector2(-50, -85);
                }

                if (txtBuildingName != null) txtBuildingName.text = "Đang Chọn Vị Trí";
                if (txtBuildingLevel != null) txtBuildingLevel.text = "";
                if (txtBuildingStatus != null) txtBuildingStatus.text = "Di chuyển đến ô đất trống...";
                return;
            }

            // Tắt nút đặt nhà nếu ở chế độ bình thường
            if (_btnConfirmPlacement != null) _btnConfirmPlacement.gameObject.SetActive(false);
            if (_btnCancelPlacement != null) _btnCancelPlacement.gameObject.SetActive(false);

            if (_currentBuilding == null || _currentBuilding.Data == null) return;

            if (txtBuildingName != null) txtBuildingName.text = _currentBuilding.Data.BuildingNameKey;
            if (txtBuildingLevel != null) txtBuildingLevel.text = $"Lv.{_currentBuilding.CurrentLevel}";
            
            string statusStr = GetStatusString(_currentBuilding.CurrentState);
            if (txtBuildingStatus != null) txtBuildingStatus.text = statusStr;

            var state = _currentBuilding.CurrentState;
            bool isProducer = _currentBuilding.Data is ProductionBuildingData;
            bool canHarvest = _currentBuilding.HasResourcesToHarvest();

            if (btnHarvest != null)
            {
                bool showHarvest = isProducer && (state == BuildingState.Producing || state == BuildingState.ReadyToHarvest);
                btnHarvest.gameObject.SetActive(showHarvest);
                btnHarvest.interactable = canHarvest;
            }

            if (btnUpgrade != null)
            {
                bool showUpgrade = (state == BuildingState.Normal || state == BuildingState.Producing || state == BuildingState.ReadyToHarvest);
                btnUpgrade.gameObject.SetActive(showUpgrade);
                btnUpgrade.interactable = true;
            }

            if (btnSpeedUp != null)
            {
                bool showSpeedUp = (state == BuildingState.Building || state == BuildingState.Upgrading);
                btnSpeedUp.gameObject.SetActive(showSpeedUp);
            }

            if (btnRepair != null)
            {
                bool showRepair = (state == BuildingState.Broken);
                btnRepair.gameObject.SetActive(showRepair);
            }

            if (btnCancel != null)
            {
                bool showCancel = (state == BuildingState.Building || state == BuildingState.Upgrading);
                btnCancel.gameObject.SetActive(showCancel);
            }

            if (btnMove != null)
            {
                bool showMove = (state == BuildingState.Normal || state == BuildingState.Producing || state == BuildingState.ReadyToHarvest);
                btnMove.gameObject.SetActive(showMove);
            }

            if (btnDemolish != null)
            {
                bool showDemolish = (state == BuildingState.Normal || state == BuildingState.Broken || 
                                     state == BuildingState.Producing || state == BuildingState.ReadyToHarvest);
                btnDemolish.gameObject.SetActive(showDemolish);
            }

            if (btnInfo != null)
            {
                btnInfo.gameObject.SetActive(true);
            }

            if (useRadialLayout)
            {
                ApplyRadialLayout();
            }
        }

        private void ApplyRadialLayout()
        {
            var activeButtons = new System.Collections.Generic.List<RectTransform>();
            Button[] allButtons = { btnInfo, btnUpgrade, btnHarvest, btnSpeedUp, btnRepair, btnCancel, btnMove, btnDemolish };

            foreach (var btn in allButtons)
            {
                if (btn != null && btn.gameObject.activeSelf)
                {
                    activeButtons.Add(btn.transform as RectTransform);
                }
            }

            int count = activeButtons.Count;
            if (count == 0) return;

            float angleStep = 360f / count;

            for (int i = 0; i < count; i++)
            {
                float angleDeg = startAngleDegrees + i * angleStep;
                float angleRad = angleDeg * Mathf.Deg2Rad;

                float x = Mathf.Cos(angleRad) * radialRadius;
                float y = Mathf.Sin(angleRad) * radialRadius;

                activeButtons[i].anchoredPosition = new Vector2(x, y);
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
            
            btnUpgrade.interactable = false;
            
            try
            {
                string code = _currentBuilding.Data.BuildingID;
                if (string.IsNullOrEmpty(code))
                {
                    GameClient.UIManager.Instance.ShowMessage("Lỗi", "Tòa nhà chưa được đồng bộ với Server!");
                    return;
                }

                var resp = await SectBuildingApi.UpgradeBuildingAsync(code);
                
                if (resp != null)
                {
                    GameClient.UIManager.Instance.ShowMessage("Thành Công", $"Đã bắt đầu nâng cấp toà nhà {_currentBuilding.Data.BuildingNameKey}!");
                    
                    // Cập nhật cấp độ cho BuildingInstance hiện tại để hiển thị visual mới
                    _currentBuilding.SetLevel(_currentBuilding.CurrentLevel + 1);
                    
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
                GameClient.UIManager.Instance.ShowMessage("Lỗi Nâng Cấp", ex.Message);
            }
            finally
            {
                if (btnUpgrade != null) btnUpgrade.interactable = true;
            }
        }

        private async void OnHarvestClicked()
        {
            if (_currentBuilding == null || !_currentBuilding.HasResourcesToHarvest()) return;
            
            btnHarvest.interactable = false;
            
            try
            {
                string code = _currentBuilding.Data.BuildingID;
                var resp = await SectBuildingApi.CollectResourcesAsync(code);
                
                if (resp != null)
                {
                    GameClient.UIManager.Instance.ShowMessage("Thu Hoạch Thành Công", $"Nhận được {resp.GoldGained} Vàng!");
                    
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
            finally
            {
                if (btnHarvest != null) btnHarvest.interactable = true;
            }
        }

        private void OnSpeedUpClicked()
        {
            GameClient.UIManager.Instance.ShowMessage("Tăng Tốc", $"Tính năng Tăng Tốc cho {_currentBuilding.Data.BuildingNameKey} đang phát triển!");
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
            GameClient.UIManager.Instance.ShowMessage(
                "Tháo Dỡ", 
                $"Bạn có chắc chắn muốn tháo dỡ {_currentBuilding.Data.BuildingNameKey} không?",
                () => {
                    BaseBuildingManager.Instance.RemoveBuilding(_currentBuilding);
                    Hide();
                }
            );
        }
    }
}
