using GameClient.Core;
using UnityEngine;
using UnityEngine.UI;
using GameClient.UI.Core;
using GameClient.Network.Pb;
using GameClient.Managers;
using System.Collections.Generic;
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
        [SerializeField] private Button levelUpButton;
        [SerializeField] private Button closeButton;

        [Header("Skills UI (3 Slots)")]
        [SerializeField] private GameObject[] skillContainers; // 3 khung hiển thị
        [SerializeField] private TMP_Text[] skillTypeTexts;     // Loại skill (Active, Passive, Ultimate)
        [SerializeField] private TMP_Text[] skillNameTexts;     // Tên skill
        [SerializeField] private TMP_Text[] skillDescTexts;     // Mô tả skill

        private Hero _currentHero;

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

            // Lấy cấu hình tướng để lấy mã định danh code (ví dụ: LIGHT_DEITY_01)
            var config = HeroDataManager.Instance.GetHeroConfigByCodeOrName(_currentHero.Name);
            string heroCode = (config != null) ? config.code : _currentHero.Name;

            // 1. Tên tướng (Dùng bảng Hero_Data để dịch tên nếu có cấu hình)
            string localizedName = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.HERO_DATA, heroCode);
            if (string.IsNullOrEmpty(localizedName) || localizedName.StartsWith("["))
            {
                localizedName = _currentHero.Name;
            }
            if (heroNameText != null) heroNameText.text = localizedName;

            // 2. Cấp độ (Dùng i18n bảng UI_System)
            string levelLabel = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_level");
            if (string.IsNullOrEmpty(levelLabel) || levelLabel.StartsWith("[")) levelLabel = "Lv.";
            if (levelText != null) levelText.text = $"{levelLabel} {_currentHero.Level}";

            // 3. Lực chiến
            string powerLabel = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_power");
            if (string.IsNullOrEmpty(powerLabel) || powerLabel.StartsWith("[")) powerLabel = "Lực chiến";
            if (powerText != null) powerText.text = $"{powerLabel}: {_currentHero.Power}";

            // 4. Độ hiếm
            if (rarityText != null) 
            {
                rarityText.text = $"[{_currentHero.Rarity}]";
                if (_currentHero.Rarity == "UR") rarityText.color = new Color(0.9f, 0.2f, 0.2f); // Đỏ
                else if (_currentHero.Rarity == "SSR") rarityText.color = new Color(0.9f, 0.5f, 0.15f); // Vàng Cam
                else if (_currentHero.Rarity == "SR") rarityText.color = new Color(0.6f, 0.35f, 0.71f); // Tím
                else rarityText.color = new Color(0.2f, 0.6f, 0.86f); // Xanh dương
            }

            // 5. Thiên phú / Traits (Dịch bằng bảng Hero_Data)
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
                string traitsLabel = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_traits");
                if (string.IsNullOrEmpty(traitsLabel) || traitsLabel.StartsWith("[")) traitsLabel = "Thiên phú";
                traitText.text = $"{traitsLabel}: " + (translatedTraits.Count > 0 ? string.Join(", ", translatedTraits) : "Không");
            }

            // 6. Ảnh chân dung
            if (heroImage != null)
            {
                if (config != null && !string.IsNullOrEmpty(config.iconAddress))
                {
                    LoadAndSetAvatar(heroImage, config.iconAddress);
                }
                else
                {
                    LoadAndSetAvatar(heroImage, _currentHero.Name);
                }
            }

            // 7. Thiết lập 3 kỹ năng (Active, Passive, Ultimate) dịch bằng bảng Skill_Description
            SetupSkills(heroCode);

            // Nút nâng cấp
            if (levelUpButton != null)
            {
                var txtBtn = levelUpButton.GetComponentInChildren<TMP_Text>();
                if (txtBtn != null)
                {
                    string lvlUpLabel = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_level_up");
                    if (string.IsNullOrEmpty(lvlUpLabel) || lvlUpLabel.StartsWith("[")) lvlUpLabel = "Nâng cấp";
                    txtBtn.text = lvlUpLabel;
                }
            }
        }

        private void SetupSkills(string heroCode)
        {
            // Các loại kỹ năng:
            // 1. Kỹ năng chủ động (Active Skill)
            // 2. Kỹ năng bị động (Passive Skill)
            // 3. Tuyệt kỹ / Nộ (Ultimate Skill)
            string[] skillTypes = { "skill_type_active", "skill_type_passive", "skill_type_ultimate" };

            for (int i = 0; i < 3; i++)
            {
                if (skillContainers != null && i < skillContainers.Length && skillContainers[i] != null)
                {
                    skillContainers[i].SetActive(true);

                    // A. Dịch loại kỹ năng (Bảng UI_System)
                    if (skillTypeTexts != null && i < skillTypeTexts.Length && skillTypeTexts[i] != null)
                    {
                        string locType = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, skillTypes[i]);
                        if (string.IsNullOrEmpty(locType) || locType.StartsWith("["))
                        {
                            locType = i == 0 ? "Chủ động" : i == 1 ? "Bị động" : "Tuyệt kỹ";
                        }
                        skillTypeTexts[i].text = locType;
                    }

                    // B. Dịch tên kỹ năng (Bảng Skill_Description)
                    if (skillNameTexts != null && i < skillNameTexts.Length && skillNameTexts[i] != null)
                    {
                        string nameKey = $"{heroCode}_skill_{i + 1}_name";
                        string locName = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.SKILL_DES, nameKey);
                        if (string.IsNullOrEmpty(locName) || locName.StartsWith("["))
                        {
                            locName = $"Kỹ năng {i + 1}";
                        }
                        skillNameTexts[i].text = locName;
                    }

                    // C. Dịch mô tả kỹ năng (Bảng Skill_Description)
                    if (skillDescTexts != null && i < skillDescTexts.Length && skillDescTexts[i] != null)
                    {
                        string descKey = $"{heroCode}_skill_{i + 1}_desc";
                        string locDesc = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.SKILL_DES, descKey);
                        if (string.IsNullOrEmpty(locDesc) || locDesc.StartsWith("["))
                        {
                            locDesc = "Mô tả chi tiết kỹ năng chưa được cập nhật.";
                        }
                        skillDescTexts[i].text = locDesc;
                    }
                }
            }
        }

        private async void LoadAndSetAvatar(Image img, string address)
        {
            if (string.IsNullOrEmpty(address) || address.Contains(" "))
            {
                return;
            }
            try
            {
                var sprite = await ResourceManager.Instance.LoadAssetAsync<Sprite>(address);
                if (sprite != null && img != null)
                {
                    img.sprite = sprite;
                }
            }
            catch (System.Exception)
            {
                // Fallback
            }
        }

        private async void OnLevelUpClicked()
        {
            if (_currentHero == null) return;
            
            levelUpButton.interactable = false;
            Debug.Log($"[HeroDetail] Requesting Level Up for Hero {_currentHero.Id}");
            
            try
            {
                var response = await GameClient.Network.Api.DiscipleApi.LevelUpHeroAsync(_currentHero.Id);
                if (response != null && response.Base.Code == 0 && response.Hero != null)
                {
                    Debug.Log($"[HeroDetail] Cấp độ Tướng cập nhật: {response.Hero.Level}");
                    _currentHero = response.Hero; // Cập nhật data mới
                    RefreshUI();
                }
                else
                {
                    Debug.LogWarning($"[HeroDetail] Nâng cấp thất bại: {response?.Base?.Message}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HeroDetail] Lỗi gọi API nâng cấp: {ex.Message}");
            }
            finally
            {
                levelUpButton.interactable = true;
            }
        }
    }
}
