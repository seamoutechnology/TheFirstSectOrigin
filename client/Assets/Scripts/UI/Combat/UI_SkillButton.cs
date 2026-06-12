using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GameClient.UI.Combat
{
    public class UI_SkillButton : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text txtName;       // Text hiển thị tên kỹ năng
        [SerializeField] private TMP_Text txtCooldown;   // Text hiển thị số lượt hồi chiêu (ví dụ: "10L")
        [SerializeField] private Button button;          // Nút bấm

        public Button Button => button;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            
            // Tự động tìm kiếm nếu chưa gán
            if (txtName == null)
            {
                var nameTrans = transform.Find("txtName") ?? transform.Find("NameText") ?? transform.Find("Text");
                if (nameTrans != null) txtName = nameTrans.GetComponent<TMP_Text>();
            }

            if (txtCooldown == null)
            {
                var cdTrans = transform.Find("txtCooldown") ?? transform.Find("CooldownText") ?? transform.Find("TurnText");
                if (cdTrans != null) txtCooldown = cdTrans.GetComponent<TMP_Text>();
            }
        }

        public void Setup(string skillName, int cooldown)
        {
            if (txtName != null)
            {
                txtName.text = skillName;
            }

            if (txtCooldown != null)
            {
                if (cooldown > 0)
                {
                    txtCooldown.gameObject.SetActive(true);
                    txtCooldown.text = $"{cooldown}L";
                }
                else
                {
                    txtCooldown.gameObject.SetActive(false);
                }
            }

            if (button != null)
            {
                button.interactable = (cooldown <= 0);
            }
        }
    }
}
