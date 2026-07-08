using UnityEngine;
using TMPro;
using GameClient.Gameplay.BaseBuilder;

namespace GameClient.UI
{
    public class SubPanelForge : BuildingSubPanel
    {
        [SerializeField] private TMP_Text txtCurrentWeapon;
        [SerializeField] private TMP_Text txtForgeStatus;
        [SerializeField] private UnityEngine.UI.Slider sliderForgeProgress;
        [SerializeField] private Transform forgeDisciplesContainer;
        [SerializeField] private GameObject discipleItemPrefab;

        public override void Setup(BuildingInstance building)
        {
            if (txtCurrentWeapon != null) txtCurrentWeapon.text = "Vũ khí đang đúc: Thanh Long Kiếm (Bậc 2)";
            if (sliderForgeProgress != null) sliderForgeProgress.value = 0.5f;
            if (txtForgeStatus != null) txtForgeStatus.text = "Trạng thái: Đang rèn đúc (còn 30s)";

            foreach (Transform child in forgeDisciplesContainer)
            {
                Destroy(child.gameObject);
            }

            CreateDiscipleItem("Đường Hạo", "Đang Gõ Búa (Thần lực)");
        }

        private void CreateDiscipleItem(string name, string status)
        {
            if (forgeDisciplesContainer == null) return;

            if (discipleItemPrefab != null)
            {
                GameObject go = Instantiate(discipleItemPrefab, forgeDisciplesContainer);
                var texts = go.GetComponentsInChildren<TMP_Text>();
                if (texts.Length >= 1) texts[0].text = name;
                if (texts.Length >= 2) texts[1].text = status;
            }
            else
            {
                GameObject textObj = new GameObject("DiscipleMock");
                textObj.transform.SetParent(forgeDisciplesContainer, false);
                var txt = textObj.AddComponent<TextMeshProUGUI>();
                txt.text = $"• <b>{name}</b>: {status}";
                txt.fontSize = 14;
            }
        }
    }
}
