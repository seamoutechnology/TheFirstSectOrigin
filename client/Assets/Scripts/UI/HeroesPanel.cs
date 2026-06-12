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
            _ = LoadHeroesAsync();
        }

        private async System.Threading.Tasks.Task LoadHeroesAsync()
        {
            try
            {
                var response = await GameClient.Network.Api.DiscipleApi.GetHeroesAsync();
                if (response != null && response.Base != null && response.Base.Code == 0)
                {
                    GameManager.Instance.SetHeroes(response.Heroes);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HeroesPanel] Lỗi khi tải danh sách đệ tử: {ex.Message}");
            }

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

                // Reset Z coordinate và scale để tránh bị lệch trục Z trôi ra sau background của Canvas
                var rect = go.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.localPosition = new Vector3(rect.localPosition.x, rect.localPosition.y, 0f);
                    rect.localScale = Vector3.one;
                }

                var heroRef = h; // Capture variable for closure

                // Thử lấy script UI_HeroItem để điền đầy đủ dữ liệu
                var heroItemScript = go.GetComponent<UI_HeroItem>();
                if (heroItemScript == null) heroItemScript = go.GetComponentInChildren<UI_HeroItem>();

                if (heroItemScript != null)
                {
                    heroItemScript.Setup(heroRef, () => OnHeroClicked(heroRef));
                }
                else
                {
                    // Fallback nếu prefab chưa có script UI_HeroItem được gán
                    var img = go.GetComponent<Image>();
                    if (img == null) img = go.GetComponentInChildren<Image>();

                    if (img != null)
                    {
                        var config = HeroDataManager.Instance.GetHeroConfigByCodeOrName(h.Name);
                        if (config != null && !string.IsNullOrEmpty(config.iconAddress))
                        {
                            LoadAndSetAvatar(img, config.iconAddress);
                        }
                        else
                        {
                            LoadAndSetAvatar(img, h.Name);
                        }
                    }

                    var btn = go.GetComponent<Button>();
                    if (btn == null) btn = go.AddComponent<Button>();
                    btn.onClick.AddListener(() => OnHeroClicked(heroRef));
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
                // Bỏ qua hoặc gán ảnh mặc định nếu lỗi tải
            }
        }

        private void OnHeroClicked(GameClient.Network.Pb.Hero hero)
        {
            UIManager.Instance.OpenPanel("HeroDetailPanel", hero);
        }
    }
}
