using System;
using UnityEngine;

namespace GameClient.UI.Presenters
{
    public interface IEntryView
    {
        void SetVersionText(string text);
        void SetServerInfo(string serverName, bool isOnline, Sprite statusOnline, Sprite statusOffline, Sprite statusNeutral);
        void SetEnterButtonInteractable(bool interactable);
        
        event Action OnNoticeClicked;
        event Action OnChangeServerClicked;
        event Action OnEnterGameClicked;

        void ShowNoticePanel();
        void ShowZoneSelectPanel();
    }
}
