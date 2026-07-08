using System;
using GameClient.Core.Interfaces;

namespace GameClient.UI.Presenters
{
    public interface ILoginView : IUIView
    {
        string GetUsername();
        string GetPassword();
        void SetUsername(string username);
        void ShowError(string message);
        void SetLoginInteractable(bool interactable);
        void SyncPasswordVisibility(bool isVisible);
        void SetPasswordInputType(bool isVisible);
        
        event Action OnLoginRequested;
        event Action OnRegisterRequested;
        event Action<bool> OnPasswordVisibilityToggled;
    }
}
