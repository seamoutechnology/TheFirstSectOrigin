using GameClient.Core;
using UnityEngine;
using UnityEngine.UI;
using GameClient.UI.Core;
using GameClient.Managers;
using GameClient.Network.Pb;

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
                go.SetActive(true);

                // Tìm component Image để hiển thị chân dung tướng
                var img = go.GetComponent<Image>();
                if (img == null) img = go.GetComponentInChildren<Image>();

                if (img != null)
                {
                    // Lấy cấu hình tướng để lấy đường dẫn Avatar/Icon
                    var config = HeroDataManager.Instance.GetHeroConfigByCodeOrName(h.Name);
                    if (config != null && !string.IsNullOrEmpty(config.iconAddress))
                    {
                        LoadAndSetAvatar(img, config.iconAddress);
                    }
                    else
                    {
                        // Fallback: Thử tải theo mã tướng
                        LoadAndSetAvatar(img, h.Name);
                    }
                }

                var btn = go.GetComponent<Button>();
                if (btn == null) btn = go.AddComponent<Button>();
                
                var heroRef = h; // Capture variable for closure
                btn.onClick.AddListener(() => OnHeroClicked(heroRef));
            }
        }

        private async void LoadAndSetAvatar(Image img, string address)
        {
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
                // Bỏ qua hoặc gán ảnh mặc định nếu lỗi tải
            }
        }

        private void OnHeroClicked(GameClient.Network.Pb.Hero hero)
        {
            UIManager.Instance.OpenPanel("HeroDetailPanel", hero);
        }
    }
}
