using GameClient.Core;
using UnityEngine;
using UnityEngine.UI;
using GameClient.UI.Core;
using GameClient.Network.Pb;
using GameClient.Managers;
using System.Collections.Generic;
using GameClient.Network;
using TMPro;

namespace GameClient.UI
{
    public class HeroDetailPanel : BaseUIPanel
    {
        [Header("UI References")]
        [SerializeField] private Image heroImage;
        [SerializeField] private TMP_Text heroNameText;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text powerText;
        [SerializeField] private TMP_Text rarityText;
        [SerializeField] private TMP_Text traitText; // Hiển thị các thiên phú/traits
        [SerializeField] private TMP_Text elementText; // Hiển thị hệ (Thủy Hỏa Thổ Kim Mộc)
        [SerializeField] private TMP_Text txtRequiredMaterials; // Hiển thị nguyên liệu nâng cấp cần
        [SerializeField] private Button levelUpButton;
        [SerializeField] private Button closeButton;

        [Header("Skills UI (3 Slots)")]
        [SerializeField] private GameObject[] skillContainers; // 3 khung hiển thị
        [SerializeField] private TMP_Text[] skillTypeTexts;     // Loại skill (Active, Passive, Ultimate)
        [SerializeField] private TMP_Text[] skillNameTexts;     // Tên skill
        [SerializeField] private TMP_Text[] skillDescTexts;     // Mô tả skill

        private Hero _currentHero;
        private string _traitsTextCached = "";

        protected override void OnStart()
        {
            base.OnStart();
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (levelUpButton != null) levelUpButton.onClick.AddListener(OnLevelUpClicked);
        }

        public override void Setup(object data = null)
        {
            base.Setup(data);
            if (data is Hero hero)
            {
                _currentHero = hero;
            }
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_currentHero == null) return;

            var config = HeroDataManager.Instance.GetHeroConfigByCodeOrName(_currentHero.Name);
            string heroCode = (config != null) ? config.code : _currentHero.Name;

            string localizedName = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.HERO_DATA, heroCode);
            if (string.IsNullOrEmpty(localizedName) || localizedName.StartsWith("["))
            {
                localizedName = _currentHero.Name;
            }
            if (heroNameText != null) heroNameText.text = localizedName;

            string levelLabel = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_level");
            if (string.IsNullOrEmpty(levelLabel) || levelLabel.StartsWith("[")) levelLabel = "Lv.";
            if (levelText != null) levelText.text = $"{levelLabel} {_currentHero.Level}";

            string powerLabel = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_power");
            if (string.IsNullOrEmpty(powerLabel) || powerLabel.StartsWith("[")) powerLabel = "Lực chiến";
            if (powerText != null) powerText.text = $"{powerLabel}: {_currentHero.Power}";

            if (rarityText != null) 
            {
                rarityText.text = $"[{_currentHero.Rarity}]";
                if (_currentHero.Rarity == "UR") rarityText.color = new Color(0.9f, 0.2f, 0.2f);
                else if (_currentHero.Rarity == "SSR") rarityText.color = new Color(0.9f, 0.5f, 0.15f);
                else if (_currentHero.Rarity == "SR") rarityText.color = new Color(0.6f, 0.35f, 0.71f);
                else rarityText.color = new Color(0.2f, 0.6f, 0.86f);
            }

            // Render Element (Thủy, Hỏa, Thổ, Kim, Mộc)
            if (elementText == null)
            {
                var elObj = transform.Find("elementText") ?? transform.Find("TxtElement") ?? transform.Find("Element");
                if (elObj != null) elementText = elObj.GetComponent<TMP_Text>();
            }
            if (elementText != null)
            {
                string elStr = "";
                string elementUpper = !string.IsNullOrEmpty(_currentHero.Element) ? _currentHero.Element.ToUpper() : ((config != null) ? config.element.ToUpper() : "");
                switch (elementUpper)
                {
                    case "FIRE": elStr = "Hệ: Hỏa"; break;
                    case "WATER": elStr = "Hệ: Thủy"; break;
                    case "WOOD": elStr = "Hệ: Mộc"; break;
                    case "LIGHT": elStr = "Hệ: Kim"; break;
                    case "DARK": elStr = "Hệ: Thổ"; break;
                    default: elStr = $"Hệ: {elementUpper}"; break;
                }
                elementText.text = elStr;
            }

            _traitsTextCached = "";
            if (traitText != null)
            {
                var translatedTraits = new List<string>();
                if (_currentHero.Traits != null)
                {
                    foreach (var trait in _currentHero.Traits)
                    {
                        string locTrait = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.HERO_DATA, trait);
                        if (string.IsNullOrEmpty(locTrait) || locTrait.StartsWith("[")) locTrait = trait;
                        translatedTraits.Add(locTrait);
                    }
                }
                _traitsTextCached = string.Join(", ", translatedTraits);
                string traitLabel = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_hero_traits") ?? "Thiên Phú";
                traitText.text = $"{traitLabel}: {_traitsTextCached}";
            }

            if (heroImage != null)
            {
                string iconAddr = (config != null) ? config.iconAddress : "";
                if (!string.IsNullOrEmpty(iconAddr) && !iconAddr.Contains(" "))
                {
                    _ = LoadAndSetHeroImage(iconAddr);
                }
                else
                {
                    _ = LoadAndSetHeroImage(_currentHero.Name);
                }
            }

            UpdateRequiredMaterialsText();
            UpdateSkillsUI();
        }

        private async System.Threading.Tasks.Task LoadAndSetHeroImage(string address)
        {
            try
            {
                var sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(address);
                if (sprite != null && heroImage != null)
                {
                    heroImage.sprite = sprite;
                }
            }
            catch (System.Exception)
            {
            }
        }

        private void UpdateRequiredMaterialsText()
        {
            if (txtRequiredMaterials == null) return;

            long reqGold = GetRequiredGoldForLevelUp(_currentHero.Level);
            long reqStamina = GetRequiredStaminaForLevelUp(_currentHero.Level);

            string goldColor = GameManager.Instance.CurrentPlayer != null && GameManager.Instance.CurrentPlayer.Gold >= reqGold ? "#2ECC71" : "#E74C3C";
            string staminaColor = GameManager.Instance.CurrentPlayer != null && GameManager.Instance.CurrentPlayer.Stamina >= reqStamina ? "#2ECC71" : "#E74C3C";

            string currentGold = GameManager.Instance.CurrentPlayer != null ? GameManager.Instance.CurrentPlayer.Gold.ToString() : "0";
            string currentStamina = GameManager.Instance.CurrentPlayer != null ? GameManager.Instance.CurrentPlayer.Stamina.ToString() : "0";

            txtRequiredMaterials.text = $"Cần tiêu hao:\n" +
                                        $"- Bạc: <color={goldColor}>{currentGold}/{reqGold}</color>\n" +
                                        $"- Linh Lực: <color={staminaColor}>{currentStamina}/{reqStamina}</color>";
        }

        private long GetRequiredGoldForLevelUp(int currentLevel)
        {
            return currentLevel * 150;
        }

        private long GetRequiredStaminaForLevelUp(int currentLevel)
        {
            return currentLevel * 5;
        }

        private void UpdateSkillsUI()
        {
            if (_currentHero.Skills == null || skillContainers == null) return;

            for (int i = 0; i < skillContainers.Length; i++)
            {
                if (i >= _currentHero.Skills.Count)
                {
                    if (skillContainers[i] != null) skillContainers[i].SetActive(false);
                    continue;
                }

                var skill = _currentHero.Skills[i];
                if (skillContainers[i] != null) skillContainers[i].SetActive(true);

                if (i < skillTypeTexts.Length && skillTypeTexts[i] != null)
                {
                    string skillTypeLabel = "Chủ động";
                    if (skill.EffectType == "PASSIVE") skillTypeLabel = "Bị động";
                    else if (skill.EffectType == "ULTIMATE") skillTypeLabel = "Tuyệt kỹ";
                    skillTypeTexts[i].text = $"[{skillTypeLabel}]";
                }

                if (i < skillNameTexts.Length && skillNameTexts[i] != null)
                {
                    string locSkillName = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.SKILL_DES, skill.SkillCode);
                    if (string.IsNullOrEmpty(locSkillName) || locSkillName.StartsWith("[")) locSkillName = skill.SkillCode;
                    skillNameTexts[i].text = locSkillName;
                }

                if (i < skillDescTexts.Length && skillDescTexts[i] != null)
                {
                    string locSkillDesc = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.SKILL_DES, skill.SkillCode + "_desc");
                    if (string.IsNullOrEmpty(locSkillDesc) || locSkillDesc.StartsWith("[")) locSkillDesc = $"Sát thương: {skill.DamageMultiplier}% Sức tấn công.";
                    skillDescTexts[i].text = locSkillDesc;
                }
            }
        }

        private async void OnLevelUpClicked()
        {
            if (_currentHero == null) return;

            long reqGold = GetRequiredGoldForLevelUp(_currentHero.Level);
            long reqStamina = GetRequiredStaminaForLevelUp(_currentHero.Level);

            long currentGold = GameManager.Instance.CurrentPlayer != null ? GameManager.Instance.CurrentPlayer.Gold : 0;
            long currentStamina = GameManager.Instance.CurrentPlayer != null ? GameManager.Instance.CurrentPlayer.Stamina : 0;

            if (currentGold < reqGold || currentStamina < reqStamina)
            {
                string failedTitle = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_upgrade_failed") ?? "Thất Bại";
                UIManager.Instance.ShowMessage(failedTitle, "Không đủ tài nguyên để nâng cấp đệ tử!");
                return;
            }

            try
            {
                var response = await GameClient.Network.Api.DiscipleApi.LevelUpHeroAsync(_currentHero.Id);
                if (response != null && response.Base != null && response.Base.Code == 0)
                {
                    string successTitle = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_success_title") ?? "Thành Công";
                    UIManager.Instance.ShowMessage(successTitle, "Nâng cấp đệ tử thành công!");

                    _currentHero = response.Hero;

                    if (GameManager.Instance != null)
                    {
                        var profileRes = await GameClient.Network.Api.PlayerApi.GetPlayerProfileAsync();
                        if (profileRes != null && profileRes.Base != null && profileRes.Base.Code == 0)
                        {
                            GameManager.Instance.SetPlayer(profileRes.Profile);
                        }
                        var heroInList = GameManager.Instance.PlayerHeroes.Find(h => h.Id == _currentHero.Id);
                        if (heroInList != null)
                        {
                            heroInList.Level = _currentHero.Level;
                            heroInList.Power = _currentHero.Power;
                        }
                    }

                    RefreshUI();
                }
                else
                {
                    string errMsg = response != null && response.Base != null ? response.Base.Message : "Lỗi hệ thống";
                    UIManager.Instance.ShowMessage("Lỗi", errMsg);
                }
            }
            catch (System.Exception ex)
            {
                UIManager.Instance.ShowMessage("Lỗi", $"Lỗi nâng cấp: {ex.Message}");
            }
        }
    }
}
