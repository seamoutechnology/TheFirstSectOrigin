using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameClient.UI.Core;
using GameClient.Gameplay.BaseBuilder;
using GameClient.Network.Api;
using GameClient.Managers;
using GameClient.Core;
using System.Threading.Tasks;

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
        [SerializeField] private TMP_Text txtUpgradeCostGold;
        [SerializeField] private TMP_Text txtUpgradeCostWood;
        [SerializeField] private TMP_Text txtUpgradeTime;
        [SerializeField] private GameObject costContainer;
        [SerializeField] private TMP_Text txtMaxLevelReached;

        [Header("Dynamic Sub-Panel Configuration")]
        [SerializeField] private Transform subPanelContainer; // Vùng chứa Sub-Panel
        [SerializeField] private GameObject defaultSubPanelPrefab; // Prefab mặc định dự phòng

        [Header("Control Buttons")]
        [SerializeField] private Button btnUpgrade;
        [SerializeField] private Button btnClose;

        private BuildingInstance _currentBuilding;
        private GameObject _activeSubPanelInstance;

        protected override void Awake()
        {
            base.Awake();
            
            if (btnUpgrade != null) btnUpgrade.onClick.AddListener(OnUpgradeClicked);
            if (btnClose != null) btnClose.onClick.AddListener(Hide);
        }

        public override void Setup(object data)
        {
            base.Setup(data);
            
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

            if (txtBuildingName != null) txtBuildingName.text = _currentBuilding.Data.BuildingNameKey;
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

        private void UpdateUpgradeRequirements()
        {
            int nextLevel = _currentBuilding.CurrentLevel + 1;
            var levelStatsList = _currentBuilding.Data.LevelStats;
            BuildingLevelStats nextLevelStats = levelStatsList.Find(s => s.Level == nextLevel);

            if (nextLevelStats != null)
            {
                if (costContainer != null) costContainer.SetActive(true);
                if (txtMaxLevelReached != null) txtMaxLevelReached.gameObject.SetActive(false);
                if (btnUpgrade != null) btnUpgrade.interactable = true;

                if (txtUpgradeCostGold != null) txtUpgradeCostGold.text = nextLevelStats.CostGold.ToString();
                if (txtUpgradeCostWood != null) txtUpgradeCostWood.text = nextLevelStats.CostWood.ToString();
                if (txtUpgradeTime != null) txtUpgradeTime.text = $"{nextLevelStats.BuildTimeSeconds} giây";
            }
            else
            {
                if (costContainer != null) costContainer.SetActive(false);
                if (txtMaxLevelReached != null)
                {
                    txtMaxLevelReached.gameObject.SetActive(true);
                    txtMaxLevelReached.text = "Đã đạt cấp độ tối đa";
                }
                if (btnUpgrade != null) btnUpgrade.interactable = false;
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
                string code = _currentBuilding.Data.BuildingID;
                var resp = await SectBuildingApi.UpgradeBuildingAsync(code);
                
                if (resp != null)
                {
                    GameClient.UIManager.Instance.ShowMessage("Thành Công", $"Bắt đầu nâng cấp {_currentBuilding.Data.BuildingNameKey}!");
                    
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
    }
}
