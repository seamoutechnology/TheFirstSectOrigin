using System;
using System.Collections.Generic;
using GameClient.UI;
using GameClient.Core.Interfaces;

namespace GameClient.UI.Presenters
{
    public interface INoticeView : IUIView
    {
        void ShowLoading();
        void ShowError(string message);
        void ShowEmptyMessage();
        void BuildTabs(List<NoticePanel.NoticeData> notices);
        void DisplayNoticeDetails(string title, string date, string content);
        
        event Action OnCloseRequested;
        event Action<NoticePanel.NoticeData> OnTabSelected;
    }
}
