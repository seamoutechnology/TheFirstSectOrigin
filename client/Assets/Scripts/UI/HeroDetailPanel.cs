using UnityEngine;
using UnityEngine.UI;
using GameClient.UI.Core;
using GameClient.Network.Pb;

namespace GameClient.UI
{
    public class HeroDetailPanel : BaseUIPanel
    {
        [Header("UI References")]
        [SerializeField] private Text heroNameText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text powerText;
        [SerializeField] private Text rarityText;
        [SerializeField] private Button levelUpButton;
        [SerializeField] private Button closeButton;

        private Hero _currentHero;

        protected override void OnStart()
        {
            base.OnStart();
            if (closeButton != null) closeButton.onClick.AddListener(Hide);
            if (levelUpButton != null) levelUpButton.onClick.AddListener(OnLevelUpClicked);
        }

        public void Setup(Hero hero)
        {
            _currentHero = hero;
            RefreshUI();
        }

        private void RefreshUI()
        {
            if (_currentHero == null) return;

            if (heroNameText != null) heroNameText.text = _currentHero.Name;
            if (levelText != null) levelText.text = $"Lv. {_currentHero.Level}";
            if (powerText != null) powerText.text = $"Power: {_currentHero.Power}";
            if (rarityText != null) 
            {
                rarityText.text = $"[{_currentHero.Rarity}]";
                if (_currentHero.Rarity == "UR") rarityText.color = Color.magenta;
                else if (_currentHero.Rarity == "SSR") rarityText.color = Color.yellow;
                else if (_currentHero.Rarity == "SR") rarityText.color = new Color(0.5f, 0, 1f);
                else rarityText.color = Color.blue;
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
