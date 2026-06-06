using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameClient.Network;
using UnityEngine;
using VContainer;

namespace GameClient.UI.Presenters
{
    public class NoticePresenter
    {
        private INoticeView _view;
        private readonly NetworkManager _network;

        [System.Serializable]
        private class NoticeResponse
        {
            public List<NoticePanel.NoticeData> data;
        }

        [Inject]
        public NoticePresenter(NetworkManager network)
        {
            _network = network;
        }

        public void SetView(INoticeView view)
        {
            _view = view;
        }

        public void Initialize()
        {
            _view.OnCloseRequested += HandleClose;
            _view.OnTabSelected += HandleTabSelected;

            _ = FetchNoticesAsync();
        }

        public void Dispose()
        {
            _view.OnCloseRequested -= HandleClose;
            _view.OnTabSelected -= HandleTabSelected;
        }

        private async Task FetchNoticesAsync()
        {
            _view.ShowLoading();

            try
            {
                var responseJson = await _network.PostAsync("/api/notices", new { });

                if (!string.IsNullOrEmpty(responseJson))
                {
                    string wrappedJson = "{\"data\":" + responseJson + "}";
                    var res = JsonUtility.FromJson<NoticeResponse>(wrappedJson);

                    if (res != null && res.data != null && res.data.Count > 0)
                    {
                        _view.BuildTabs(res.data);
                        HandleTabSelected(res.data[0]);
                    }
                    else
                    {
                        _view.ShowEmptyMessage();
                    }
                }
                else
                {
                    _view.ShowError("Không thể kết nối đến máy chủ thông báo.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NoticePresenter] Error fetching notices: {ex.Message}");
                _view.ShowError("Lỗi hệ thống khi tải thông báo.");
            }
        }

        private void HandleTabSelected(NoticePanel.NoticeData notice)
        {
            string dateStr = notice.start_at;
            if (DateTime.TryParse(notice.start_at, out DateTime startTime))
            {
                dateStr = startTime.ToString("dd/MM/yyyy");
            }
            
            _view.DisplayNoticeDetails(notice.title, dateStr, notice.content);
        }

        private void HandleClose()
        {
            _view.Hide();
        }
    }
}
