using UnityEngine;
using TMPro;
using UnityEngine.UI;
using GameClient.Gameplay.World;

namespace GameClient.WorldMap
{
    public class StageButtonItem : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI txtStageName;
        public TextMeshProUGUI txtRecommendPower;
        public Image imgIcon;

        public void SetData(StageData stage, int index)
        {
            if (stage == null) return;

            // 1. Cập nhật tên ải (Ví dụ: "1. Ải Khởi Đầu")
            if (txtStageName != null)
            {
                txtStageName.text = $"{index + 1}. {stage.stageName}";
            }

            // 2. Lực chiến đề cử
            if (txtRecommendPower != null)
            {
                txtRecommendPower.text = $"LC: {stage.recommendPower}";
            }

            // 3. Icon hiển thị (nếu có cấu hình)
            if (imgIcon != null && stage.mapIcon != null)
            {
                imgIcon.sprite = stage.mapIcon;
            }
        }
    }
}
