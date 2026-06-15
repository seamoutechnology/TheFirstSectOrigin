using UnityEngine;
using UnityEngine.UI;
using GameClient.Network;
using GameClient.Network.Api;
using GameClient.UI.Core;
using GameClient.Core;
using GameClient.Managers;
using Grpc.Core;

namespace GameClient.UI
{
    public class LobbyPanel : BaseUIPanel
    {
        [Header("Player Info UI")]
        [SerializeField] private Text nicknameText;
        [SerializeField] private Text levelText;
        [SerializeField] private Text goldText;
        [SerializeField] private Text diamondText;
        [SerializeField] private Text staminaText;
        [SerializeField] private Image avatarImage;

        [Header("Menu Buttons")]
        [SerializeField] private Button baseBuildingButton;
        [SerializeField] private Button heroesButton;
        [SerializeField] private Button gachaButton;
        [SerializeField] private Button combatButton;

        protected override void OnStart()
        {
            base.OnStart();
            GameManager.Instance.OnPlayerUpdated += UpdatePlayerUI;

            FetchLobbyData();

            baseBuildingButton.onClick.AddListener(async () => await UIManager.Instance.OpenPanelAsync("UI_BaseBuildingPanel"));
            heroesButton.onClick.AddListener(async () => await UIManager.Instance.OpenPanelAsync("UI_HeroesPanel"));
            gachaButton.onClick.AddListener(async () => await UIManager.Instance.OpenPanelAsync("UI_GachaPanel"));
            combatButton.onClick.AddListener(() => Log("Vào Combat (Sẽ load Scene Battle)"));

            if (GameManager.Instance.CurrentPlayer != null)
            {
                UpdatePlayerUI(GameManager.Instance.CurrentPlayer);
            }
        }

        protected override void OnCleanup()
        {
            base.OnCleanup();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPlayerUpdated -= UpdatePlayerUI;
            }
        }

        private async void FetchLobbyData()
        {
            try
            {
                var baseTask = SectBuildingApi.GetBaseAsync();
                var heroesTask = DiscipleApi.GetHeroesAsync();
                var stagesTask = SectBuildingApi.GetCompletedStagesAsync();

                await System.Threading.Tasks.Task.WhenAll(baseTask, heroesTask, stagesTask);

                if (baseTask.Result.Base.Code == 0)
                    GameManager.Instance.SetBuildings(baseTask.Result.Buildings);

                if (heroesTask.Result.Base.Code == 0)
                    GameManager.Instance.SetHeroes(heroesTask.Result.Heroes);

                if (stagesTask.Result.Base.Code == 0)
                    GameManager.Instance.SetCompletedStages(stagesTask.Result.StageIds);
            }
            catch (RpcException ex)
            {
                LogError($"Lỗi khi tải dữ liệu sảnh: {ex.Status}");
            }
        }

        private void UpdatePlayerUI(GameClient.Network.Pb.PlayerProfile player)
        {
            if (player == null) return;
            nicknameText.text = player.Nickname;
            
            string levelLabel = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_level");
            if (string.IsNullOrEmpty(levelLabel) || levelLabel.StartsWith("[")) levelLabel = "Lv.";
            levelText.text = $"{levelLabel} {player.Level}";
            
            goldText.text = GameClient.Utils.NumberUtils.FormatNumber(player.Gold);
            diamondText.text = GameClient.Utils.NumberUtils.FormatNumber(player.Diamond);
            staminaText.text = $"{GameClient.Utils.NumberUtils.FormatNumber(player.Stamina)}/{GameClient.Utils.NumberUtils.FormatNumber(player.MaxStamina)}";

            if (avatarImage != null)
            {
                string avatarAddress = string.IsNullOrEmpty(player.Avatar) ? "Icon_Button_Avatar" : player.Avatar;
                LoadAndSetAvatar(avatarImage, avatarAddress);
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
    }
}
