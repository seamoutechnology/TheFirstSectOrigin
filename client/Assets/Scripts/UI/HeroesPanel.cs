using UnityEngine;
using UnityEngine.UI;
using GameClient.UI.Core;

namespace GameClient.UI
{
    public class HeroesPanel : BaseUIPanel
    {
        [Header("UI References")]
        [SerializeField] private Transform heroListContainer;
        [SerializeField] private GameObject heroItemPrefab;
        [SerializeField] private Button closeButton;

        protected override void OnStart()
        {
            base.OnStart();
            closeButton.onClick.AddListener(Hide);
        }

        protected override void OnShow()
        {
            base.OnShow();
            RefreshUI();
        }

        private void RefreshUI()
        {
            foreach (Transform child in heroListContainer)
                Destroy(child.gameObject);

            var heroes = GameManager.Instance.PlayerHeroes;
            if (heroes == null || heroes.Count == 0) return;

            foreach (var h in heroes)
            {
                var go = Instantiate(heroItemPrefab, heroListContainer);
                var texts = go.GetComponentsInChildren<Text>();
                texts[0].text = $"[{h.Rarity}]";
                texts[1].text = h.Name;
                texts[2].text = $"Lv.{h.Level} | {h.Star} Star";

                if (h.Rarity == "UR") texts[0].color = Color.magenta;
                else if (h.Rarity == "SSR") texts[0].color = Color.yellow;
                else if (h.Rarity == "SR") texts[0].color = new Color(0.5f, 0, 1f);
                else texts[0].color = Color.blue;

                var btn = go.GetComponent<Button>();
                if (btn == null) btn = go.AddComponent<Button>();
                
                var heroRef = h; // Capture variable for closure
                btn.onClick.AddListener(() => OnHeroClicked(heroRef));
            }
        }

        private void OnHeroClicked(GameClient.Network.Pb.Hero hero)
        {
            UIManager.Instance.OpenPanel("HeroDetailPanel", hero);
        }
    }
}
