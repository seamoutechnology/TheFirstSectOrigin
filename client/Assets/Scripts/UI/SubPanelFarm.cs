using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameClient.Gameplay.BaseBuilder;

namespace GameClient.UI
{
    public class SubPanelFarm : BuildingSubPanel
    {
        [SerializeField] private TMP_Text txtFarmOutputRate;
        [SerializeField] private TMP_Text txtCropStatus;
        [SerializeField] private Slider sliderCropProgress;
        [SerializeField] private Transform farmDisciplesContainer;
        [SerializeField] private GameObject discipleItemPrefab;

        public override void Setup(BuildingInstance building)
        {
            if (txtFarmOutputRate != null)
            {
                if (building.Data is ProductionBuildingData prod)
                {
                    txtFarmOutputRate.text = $"Sản lượng: +{prod.ProductionRatePerSecond}/s";
                }
                else
                {
                    txtFarmOutputRate.text = $"Sản lượng: +{5 * building.CurrentLevel} Linh Thực/s";
                }
            }

            if (sliderCropProgress != null) sliderCropProgress.value = 0.65f;
            if (txtCropStatus != null) txtCropStatus.text = "Giai đoạn: Lúa linh thực chín (65%)";

            foreach (Transform child in farmDisciplesContainer)
            {
                Destroy(child.gameObject);
            }

            CreateDiscipleItem("Hàn Lập", "Đang Tưới Nước (Chăm chỉ)");
            CreateDiscipleItem("Đường Tam", "Đang Gieo Hạt (Chăm chỉ)");
        }

        private void CreateDiscipleItem(string name, string status)
        {
            if (farmDisciplesContainer == null) return;

            if (discipleItemPrefab != null)
            {
                GameObject go = Instantiate(discipleItemPrefab, farmDisciplesContainer);
                var texts = go.GetComponentsInChildren<TMP_Text>();
                if (texts.Length >= 1) texts[0].text = name;
                if (texts.Length >= 2) texts[1].text = status;
            }
            else
            {
                GameObject textObj = new GameObject("DiscipleMock");
                textObj.transform.SetParent(farmDisciplesContainer, false);
                var txt = textObj.AddComponent<TextMeshProUGUI>();
                txt.text = $"• <b>{name}</b>: {status}";
                txt.fontSize = 14;
            }
        }
    }
}
