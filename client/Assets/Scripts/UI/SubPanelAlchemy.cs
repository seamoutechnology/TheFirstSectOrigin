using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameClient.Gameplay.BaseBuilder;

namespace GameClient.UI
{
    public class SubPanelAlchemy : BuildingSubPanel
    {
        [SerializeField] private TMP_Text txtCurrentRecipe;
        [SerializeField] private TMP_Text txtAlchemyStatus;
        [SerializeField] private Slider sliderAlchemyProgress;
        [SerializeField] private Transform alchemyDisciplesContainer;
        [SerializeField] private GameObject discipleItemPrefab;

        public override void Setup(BuildingInstance building)
        {
            if (txtCurrentRecipe != null) txtCurrentRecipe.text = "Công thức: Bồi Nguyên Đan (Bậc 1)";
            if (sliderAlchemyProgress != null) sliderAlchemyProgress.value = 0.4f;
            if (txtAlchemyStatus != null) txtAlchemyStatus.text = "Trạng thái: Đang luyện chế (còn 45s)";

            foreach (Transform child in alchemyDisciplesContainer)
            {
                Destroy(child.gameObject);
            }

            CreateDiscipleItem("Dược Lão", "Đang Kiểm Soát Hoả Hầu (Nhập thần)");
        }

        private void CreateDiscipleItem(string name, string status)
        {
            if (alchemyDisciplesContainer == null) return;

            if (discipleItemPrefab != null)
            {
                GameObject go = Instantiate(discipleItemPrefab, alchemyDisciplesContainer);
                var texts = go.GetComponentsInChildren<TMP_Text>();
                if (texts.Length >= 1) texts[0].text = name;
                if (texts.Length >= 2) texts[1].text = status;
            }
            else
            {
                GameObject textObj = new GameObject("DiscipleMock");
                textObj.transform.SetParent(alchemyDisciplesContainer, false);
                var txt = textObj.AddComponent<TextMeshProUGUI>();
                txt.text = $"• <b>{name}</b>: {status}";
                txt.fontSize = 14;
            }
        }
    }
}
