using System.Threading.Tasks;
using GameClient.Managers;
using GameClient.Network;
using GameClient.Core;
using UnityEngine;
using VContainer;

namespace GameClient.UI.Presenters
{
    public class EntryPresenter
    {
        private readonly IEntryView _view;
        private readonly LocalizationManager _localization;
        private readonly UIManager _uiManager;
        private readonly NetworkManager _network;
        
        private ZoneSelectPanel.ZoneData _currentZone;

        [Inject]
        public EntryPresenter(
            IEntryView view, 
            LocalizationManager localization, 
            UIManager uiManager, 
            NetworkManager network)
        {
            _view = view;
            _localization = localization;
            _uiManager = uiManager;
            _network = network;
        }

        public void Initialize()
        {
            string versionText = _localization.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_game_version");
            _view.SetVersionText($"{versionText} {Application.version}");

            _view.OnNoticeClicked += () => { _uiManager.OpenPanel("NoticePanel", null, false); };
            _view.OnChangeServerClicked += () => { _uiManager.OpenPanel("ZoneSelectPanel", null, false); };
            _view.OnEnterGameClicked += HandleEnterGame;

            ZoneSelectPanel.OnGlobalZoneSelected += HandleZoneSelected;

            _view.SetEnterButtonInteractable(false);
            _ = FetchDefaultZone();
        }

        public void Dispose()
        {
            ZoneSelectPanel.OnGlobalZoneSelected -= HandleZoneSelected;
        }

        private async Task FetchDefaultZone()
        {
            var req = new ZoneSelectPanel.ZoneReq { type = "data", tab_id = "recent" };
            var response = await _network.PostAsync<ZoneSelectPanel.DataResponse>("/api/zones", req);
            
            if (response != null && response.zones != null && response.zones.Count > 0)
            {
                HandleZoneSelected(response.zones[0]);
            }
            else
            {
                string unselectedText = _localization.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_server_unselected") ?? "Chưa chọn Server";
                _view.SetServerInfo(unselectedText, false, null, null, null); // Will fallback to Neutral in view
            }
        }

        private void HandleZoneSelected(ZoneSelectPanel.ZoneData zone)
        {
            _currentZone = zone;
            _view.SetServerInfo(zone.name, zone.is_online, null, null, null);
            _view.SetEnterButtonInteractable(zone.is_online);
        }

        private void HandleEnterGame()
        {
            if (_currentZone == null) return;
            
            _view.SetEnterButtonInteractable(false);
            Debug.Log($"[EntryPresenter] Đang vào thế giới tại {_currentZone.host}:{_currentZone.port}...");
            _network.ConnectToGateway(_currentZone.host, _currentZone.port);
            
            GameContext.CurrentServerHost = _currentZone.host;
            GameContext.CurrentServerPort = _currentZone.port;
            GameContext.CurrentServerName = _currentZone.name;
            GameContext.HasCharacter = _currentZone.has_character;

            // Đóng Panel khởi đầu khi vào game
            _uiManager.ClosePanel("EntryPanel");

            if (MapManager.Instance != null)
            {
                _ = MapManager.Instance.LoadMapAsync(MapType.LocalBase);
            }
            else
            {
                Debug.LogError("[EntryPresenter] Không tìm thấy MapManager để load Scene!");
                UnityEngine.SceneManagement.SceneManager.LoadScene("LocalBase");
            }
        }
    }
}
