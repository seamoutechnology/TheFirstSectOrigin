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
        [SerializeField] private TMP_Text txtStarCount;
        [SerializeField] private GameObject starContainer;
        [SerializeField] private Button btnSelect;
        [SerializeField] private TMP_Text txtElement;

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
            }
            if (btnSelect != null)
            {
                btnSelect.onClick.RemoveAllListeners();
                btnSelect.onClick.AddListener(() => _onClick?.Invoke());
            }

            var config = HeroDataManager.Instance.GetHeroConfigByCodeOrName(hero.Name);
            string heroCode = (config != null) ? config.code : hero.Name;
            string localizedName = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.HERO_DATA, heroCode);
            if (string.IsNullOrEmpty(localizedName) || localizedName.StartsWith("["))
            {
                localizedName = hero.Name;
            }
            if (txtName != null) txtName.text = localizedName;

            string levelLabel = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_level");
            if (string.IsNullOrEmpty(levelLabel) || levelLabel.StartsWith("[")) levelLabel = "Lv.";
            string levelText = $"{levelLabel} {hero.Level}";
            if (txtLevel != null) txtLevel.text = levelText;

            string rarityText = hero.Rarity;
            if (txtRarity != null) txtRarity.text = rarityText;

            // Render Element (Thủy, Hỏa, Thổ, Kim, Mộc)
            if (txtElement == null)
            {
                var elObj = transform.Find("txtElement") ?? transform.Find("TxtElement") ?? transform.Find("Element");
                if (elObj != null) txtElement = elObj.GetComponent<TMP_Text>();
            }
            if (txtElement != null)
            {
                string elStr = "";
                string elementUpper = !string.IsNullOrEmpty(hero.Element) ? hero.Element.ToUpper() : ((config != null) ? config.element.ToUpper() : "");
                switch (elementUpper)
                {
                    case "FIRE": elStr = "Hỏa"; break;
                    case "WATER": elStr = "Thủy"; break;
                    case "WOOD": elStr = "Mộc"; break;
                    case "LIGHT": elStr = "Kim"; break;
                    case "DARK": elStr = "Thổ"; break;
                    default: elStr = elementUpper; break;
                }
                txtElement.text = elStr;
            }

            if (txtStarCount != null)
            {
                txtStarCount.text = hero.Star.ToString();
            }
            if (starContainer != null)
            {
                starContainer.SetActive(hero.Star > 0);
            }

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
                }
            }

            if (rect != null)
            {
                rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, 0f);
                rect.localScale = Vector3.one;
            }

            if (imgIcon != null && sprite != null)
            {
                imgIcon.sprite = sprite;
            }

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
