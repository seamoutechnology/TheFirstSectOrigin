using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using GameClient.Managers;
using GameClient.Core;
using GameClient.Network.Pb;

namespace GameClient.UI
{
    public class UI_HeroItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image imgIcon;
        [SerializeField] private Image imgBorder;
        [SerializeField] private TMP_Text txtName;
        [SerializeField] private TMP_Text txtLevel;
        [SerializeField] private TMP_Text txtRarity;
        [SerializeField] private TMP_Text txtStarCount; // Đối tượng Text hiển thị số sao (ví dụ: "5")
        [SerializeField] private GameObject starContainer; // Panel cha chứa cả Text số sao và hình ảnh ngôi sao
        [SerializeField] private Button btnSelect;

        private Hero _hero;
        private Action _onClick;



        public async void Setup(Hero hero, Action onClick)
        {
            var rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, 0f);
                rect.localScale = Vector3.one;
            }

            _hero = hero;
            _onClick = onClick;

            // Đảm bảo tìm thấy nút và đăng ký sự kiện click ngay trong Setup
            if (btnSelect == null)
            {
                btnSelect = GetComponent<Button>();
            }
            if (btnSelect == null)
            {
                btnSelect = GetComponentInChildren<Button>();
            }
            if (btnSelect == null)
            {
                btnSelect = gameObject.AddComponent<Button>();
                Debug.Log("[UI_HeroItem] Tự động thêm component Button cho vật phẩm tướng vì prefab không có.");
            }
            if (btnSelect != null)
            {
                btnSelect.onClick.RemoveAllListeners();
                btnSelect.onClick.AddListener(() => _onClick?.Invoke());
            }

            // Thiết lập tên (Dịch bằng bảng Hero_Data)
            var config = HeroDataManager.Instance.GetHeroConfigByCodeOrName(hero.Name);
            string heroCode = (config != null) ? config.code : hero.Name;
            string localizedName = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.HERO_DATA, heroCode);
            if (string.IsNullOrEmpty(localizedName) || localizedName.StartsWith("["))
            {
                localizedName = hero.Name;
            }
            if (txtName != null) txtName.text = localizedName;

            // Thiết lập cấp độ (Dịch bằng bảng UI_System)
            string levelLabel = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_level");
            if (string.IsNullOrEmpty(levelLabel) || levelLabel.StartsWith("[")) levelLabel = "Lv.";
            string levelText = $"{levelLabel} {hero.Level}";
            if (txtLevel != null) txtLevel.text = levelText;

            // Thiết lập độ hiếm
            string rarityText = hero.Rarity;
            if (txtRarity != null) txtRarity.text = rarityText;

            // Thiết lập hiển thị sao (số lượng sao và bật/tắt container)
            if (txtStarCount != null)
            {
                txtStarCount.text = hero.Star.ToString();
            }
            if (starContainer != null)
            {
                starContainer.SetActive(hero.Star > 0);
            }

            // Thiết lập ảnh chân dung
            Sprite sprite = null;
            string address = (config != null && !string.IsNullOrEmpty(config.iconAddress)) ? config.iconAddress : "";

            if (!string.IsNullOrEmpty(address) && !address.Contains(" "))
            {
                try
                {
                    sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(address);
                }
                catch (Exception)
                {
                    // Fallback nếu lỗi load addressable
                }
            }

            // Reset Z coordinate lần nữa sau khi await (do layout group có thể đã tính toán lại và thay đổi vị trí Z)
            if (rect != null)
            {
                rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, 0f);
                rect.localScale = Vector3.one;
            }

            if (imgIcon != null && sprite != null)
            {
                imgIcon.sprite = sprite;
            }

            // Thiết lập màu viền dựa theo độ hiếm
            if (imgBorder != null)
            {
                Color borderColor = Color.white; // Common
                switch (hero.Rarity.ToUpper())
                {
                    case "R":
                        borderColor = new Color(0.2f, 0.6f, 0.86f); // Blue
                        break;
                    case "SR":
                        borderColor = new Color(0.6f, 0.35f, 0.71f); // Purple
                        break;
                    case "SSR":
                        borderColor = new Color(0.9f, 0.5f, 0.15f); // Orange/Gold
                        break;
                    case "UR":
                        borderColor = new Color(0.9f, 0.2f, 0.2f); // Red
                        break;
                }
                imgBorder.color = borderColor;
            }
        }
    }
}
