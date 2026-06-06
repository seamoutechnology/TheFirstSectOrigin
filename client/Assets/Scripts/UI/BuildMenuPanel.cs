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

        protected override void OnStart()
        {
            base.OnStart();
            if (_closeButton != null) _closeButton.onClick.AddListener(Hide);
        }

        protected override void OnShow()
        {
            base.OnShow();
            _ = PopulateBuildingListAsync();
        }

        private async Task PopulateBuildingListAsync()
        {
            foreach (Transform child in _buildingListContainer)
            {
                Destroy(child.gameObject);
            }

            var database = BaseBuildingManager.Instance.GetBuildingDatabase();
            foreach (var kvp in database)
            {
                var buildingData = kvp.Value;
                
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

                SetupBuildingButton(itemGo, buildingData);
            }
        }

        private void SetupBuildingButton(GameObject btnGo, BuildingData buildingData)
        {
            var texts = btnGo.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
            if (texts.Length > 0)
            {
                int reqReputation = 0;
                if (buildingData.LevelStats != null && buildingData.LevelStats.Count > 0)
                {
                    reqReputation = buildingData.LevelStats[0].RequiredReputation;
                }
                
                string reqText = reqReputation > 0 ? $" (Cần {reqReputation} Danh Tiếng)" : "";
                texts[0].text = buildingData.BuildingNameKey + reqText; // Có thể gắn Localization sau
            }

            var btn = btnGo.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnBuildingButtonClicked(buildingData));
            }
        }

        private void OnBuildingButtonClicked(BuildingData data)
        {
            int reqReputation = 0;
            if (data.LevelStats != null && data.LevelStats.Count > 0)
            {
                reqReputation = data.LevelStats[0].RequiredReputation;
            }

            if (GameContext.SectReputation < reqReputation)
            {
                Debug.LogWarning($"[BuildMenuPanel] Không đủ danh tiếng! Cần {reqReputation} nhưng hiện có {GameContext.SectReputation}.");
                // TODO: Hiển thị floating text cảnh báo trên màn hình
                return;
            }

            Debug.Log($"[BuildMenuPanel] Đã chọn xây nhà: {data.BuildingID}");
            Hide(); // Ẩn Menu sau khi chọn xong

            var controller = FindFirstObjectByType<GameClient.BaseBuilding.Core.BuildingController>();
            if (controller != null)
            {
                controller.StartPlacement(data.BuildingID, data.PrefabAddress, data.SizeX, data.SizeY);
            }
            else
            {
                Debug.LogError("[BuildMenuPanel] Không tìm thấy BuildingController trong Scene!");
            }
        }
    }
}
