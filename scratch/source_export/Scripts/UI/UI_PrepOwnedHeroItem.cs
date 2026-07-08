using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using GameClient.Managers;
using GameClient.Core;
using GameClient.Network.Pb;

namespace GameClient.UI
{
    public class UI_PrepOwnedHeroItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image imgIcon;
        [SerializeField] private Image imgBorder;
        [SerializeField] private TMP_Text txtName;
        [SerializeField] private TMP_Text txtLevel;
        [SerializeField] private TMP_Text txtRarity;
        [SerializeField] private TMP_Text txtPower;        // Đối tượng Text hiển thị Chiến Lực của tướng
        [SerializeField] private GameObject placedOverlay; // Overlay hiển thị khi tướng đã ra trận

        private Action _onClick;

        public Button Button { get; private set; }

        private void Awake()
        {
            Button = GetComponent<Button>();
            if (Button == null) Button = gameObject.AddComponent<Button>();
            
            Button.onClick.RemoveAllListeners();
            Button.onClick.AddListener(() => _onClick?.Invoke());
        }

        public async void Setup(Hero hero, bool isPlaced, Action onClick)
        {
            _onClick = onClick;

            // Thiết lập tên (Dịch bằng bảng Hero_Data)
            var config = HeroDataManager.Instance.GetHeroConfigByCodeOrName(hero.Name);
            string heroCode = (config != null) ? config.code : hero.Name;
            string localizedName = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.HERO_DATA, heroCode);
            if (string.IsNullOrEmpty(localizedName) || localizedName.StartsWith("["))
            {
                localizedName = hero.Name;
            }
            if (txtName != null) txtName.text = localizedName;

            // Thiết lập cấp độ
            string levelLabel = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_level");
            if (string.IsNullOrEmpty(levelLabel) || levelLabel.StartsWith("[")) levelLabel = "Lv.";
            if (txtLevel != null) txtLevel.text = $"{levelLabel} {hero.Level}";

            // Thiết lập độ hiếm
            if (txtRarity != null) txtRarity.text = hero.Rarity;

            // Thiết lập chiến lực
            if (txtPower != null) txtPower.text = hero.Power.ToString();

            // Trạng thái đã lên trận (Đặt overlay/tắt nút hoặc làm mờ)
            if (placedOverlay != null)
            {
                placedOverlay.SetActive(isPlaced);
            }

            // Tải ảnh chân dung tướng
            Sprite sprite = null;
            string address = (config != null && !string.IsNullOrEmpty(config.iconAddress)) ? config.iconAddress : "";

            if (!string.IsNullOrEmpty(address) && !address.Contains(" "))
            {
                try
                {
                    sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(address);
                    if (imgIcon != null && sprite != null)
                    {
                        imgIcon.sprite = sprite;
                    }
                }
                catch (Exception)
                {
                    // Fallback nếu lỗi
                }
            }

            // Thiết lập màu viền dựa theo độ hiếm
            if (imgBorder != null)
            {
                Color borderColor = Color.white;
                switch (hero.Rarity.ToUpper())
                {
                    case "R":
                        borderColor = new Color(0.2f, 0.6f, 0.86f);
                        break;
                    case "SR":
                        borderColor = new Color(0.6f, 0.35f, 0.71f);
                        break;
                    case "SSR":
                        borderColor = new Color(0.9f, 0.5f, 0.15f);
                        break;
                    case "UR":
                        borderColor = new Color(0.9f, 0.2f, 0.2f);
                        break;
                }
                imgBorder.color = borderColor;
            }
        }
    }
}
