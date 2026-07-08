using System;
using System.Collections.Generic;
using GameClient.UI;
using GameClient.Core.Interfaces;

namespace GameClient.UI.Presenters
{
    public interface IZoneSelectView : IUIView
    {
        void ShowLoading();
        void ShowError(string message);
        void BuildTabs(List<ZoneSelectPanel.TabInfo> tabs, string defaultTabId);
        void HighlightTab(string tabId);
        void RenderServers(List<ZoneSelectPanel.ZoneData> zones);
        void ClearMainContent();
        
        event Action OnCloseRequested;
        event Action<string, string> OnTabSelected;
        event Action<ZoneSelectPanel.ZoneData> OnZoneSelected;
    }
}
