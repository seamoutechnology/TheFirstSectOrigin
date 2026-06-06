using UnityEngine;
using TMPro;
using GameClient.Gameplay.BaseBuilder;

namespace GameClient.UI
{
    public class SubPanelResidence : BuildingSubPanel
    {
        [SerializeField] private TMP_Text txtResidenceCapacity;
        [SerializeField] private TMP_Text txtRestRate;
        [SerializeField] private Transform residenceDisciplesContainer;
        [SerializeField] private GameObject discipleItemPrefab;

        public override void Setup(BuildingInstance building)
        {
            int maxCapacity = 2 + building.CurrentLevel * 2; // Ví dụ cấp 1 chứa 4 đệ tử, cấp 2 chứa 6...
            if (txtResidenceCapacity != null) txtResidenceCapacity.text = $"Sức chứa: 2/{maxCapacity} Đệ tử";
            if (txtRestRate != null) txtRestRate.text = $"Tốc độ hồi thể lực: +{10 + building.CurrentLevel * 5}/giờ";

            foreach (Transform child in residenceDisciplesContainer)
            {
                Destroy(child.gameObject);
            }

            CreateDiscipleItem("Lâm Phong", "Đang Nghỉ Ngơi (Thể lực: 82%)");
            CreateDiscipleItem("Tiêu Viêm", "Đang Tu Dưỡng (Thể lực: 95%)");
        }

        private void CreateDiscipleItem(string name, string status)
        {
            if (residenceDisciplesContainer == null) return;

            if (discipleItemPrefab != null)
            {
                GameObject go = Instantiate(discipleItemPrefab, residenceDisciplesContainer);
                var texts = go.GetComponentsInChildren<TMP_Text>();
                if (texts.Length >= 1) texts[0].text = name;
                if (texts.Length >= 2) texts[1].text = status;
            }
            else
            {
                GameObject textObj = new GameObject("DiscipleMock");
                textObj.transform.SetParent(residenceDisciplesContainer, false);
                var txt = textObj.AddComponent<TextMeshProUGUI>();
                txt.text = $"• <b>{name}</b>: {status}";
                txt.fontSize = 14;
            }
        }
    }
}
