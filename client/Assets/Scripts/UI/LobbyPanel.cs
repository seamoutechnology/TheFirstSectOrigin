using UnityEngine;
using UnityEngine.UI;
using GameClient.Network;
using GameClient.Network.Api;
using GameClient.UI.Core;
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

                await System.Threading.Tasks.Task.WhenAll(baseTask, heroesTask);

                if (baseTask.Result.Base.Code == 0)
                    GameManager.Instance.SetBuildings(baseTask.Result.Buildings);

                if (heroesTask.Result.Base.Code == 0)
                    GameManager.Instance.SetHeroes(heroesTask.Result.Heroes);
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
            levelText.text = $"Lv. {player.Level}";
            goldText.text = player.Gold.ToString("N0");
            diamondText.text = player.Diamond.ToString("N0");
            staminaText.text = $"{player.Stamina}/{player.MaxStamina}";
        }
    }
}
