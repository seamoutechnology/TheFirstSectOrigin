using System;
using System.Threading.Tasks;
using GameClient.Network;
using GameClient.Managers;
using UnityEngine;
using VContainer;

namespace GameClient.UI.Presenters
{
    public class ZoneSelectPresenter
    {
        private IZoneSelectView _view;
        private readonly NetworkManager _network;
        private readonly UIManager _uiManager;

        private ZoneSelectPanel.MetaResponse _metaResponse;

        [Inject]
        public ZoneSelectPresenter(NetworkManager network, UIManager uiManager)
        {
            _network = network;
            _uiManager = uiManager;
        }

        public void SetView(IZoneSelectView view)
        {
            _view = view;
        }

        public void Initialize()
        {
            _view.OnCloseRequested += HandleClose;
            _view.OnTabSelected += HandleTabSelected;
            _view.OnZoneSelected += HandleZoneSelected;

            _ = FetchAllZonesAsync();
        }

        public void Dispose()
        {
            _view.OnCloseRequested -= HandleClose;
            _view.OnTabSelected -= HandleTabSelected;
            _view.OnZoneSelected -= HandleZoneSelected;
        }

        private async Task FetchAllZonesAsync()
        {
            _view.ShowLoading();

            try
            {
                var reqData = new ZoneSelectPanel.ZoneReq { type = "meta", tab_id = "" };
                var metaRes = await _network.PostAsync<ZoneSelectPanel.MetaResponse>("/api/zones", reqData);

                if (metaRes != null && metaRes.tabs != null && metaRes.tabs.Count > 0)
                {
                    _metaResponse = metaRes;
                    _view.BuildTabs(metaRes.tabs, metaRes.tabs[0].id);
                    await FetchZoneDataAsync(metaRes.tabs[0].id);
                }
                else
                {
                    Debug.LogError("[ZoneSelectPresenter] API returned empty or failed to parse.");
                    _view.ShowError("Lỗi kết nối hoặc không có dữ liệu máy chủ.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ZoneSelectPresenter] Error fetching meta zones: {ex.Message}");
                _view.ShowError("Lỗi hệ thống khi tải danh sách máy chủ.");
            }
        }

        private async void HandleTabSelected(string tabId, string tabName)
        {
            _view.HighlightTab(tabId);
            _view.ClearMainContent();
            await FetchZoneDataAsync(tabId);
        }

        private async Task FetchZoneDataAsync(string tabId)
        {
            try
            {
                var reqData = new ZoneSelectPanel.ZoneReq { type = "data", tab_id = tabId };
                var dataRes = await _network.PostAsync<ZoneSelectPanel.DataResponse>("/api/zones", reqData);

                if (dataRes != null && dataRes.zones != null)
                {
                    _view.RenderServers(dataRes.zones);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ZoneSelectPresenter] Error fetching zone data: {ex.Message}");
            }
        }

        private void HandleZoneSelected(ZoneSelectPanel.ZoneData zone)
        {
            ZoneSelectPanel.NotifyZoneSelected(zone);
            _view.Hide();
        }

        private void HandleClose()
        {
            _view.Hide();
        }
    }
}
