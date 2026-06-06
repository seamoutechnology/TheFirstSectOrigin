using System;
using GameClient.Core.Interfaces;

namespace GameClient.UI.Presenters
{
    public interface IRegisterView : IUIView
    {
        string GetUsername();
        string GetEmail();
        string GetPassword();
        string GetConfirmPassword();
        
        void ShowError(string message);
        void SetRegisterInteractable(bool interactable);
        void SyncPasswordVisibility(bool isVisible);
        void SetPasswordInputType(bool isVisible);
        
        event Action OnRegisterRequested;
        event Action OnBackRequested;
        event Action<bool> OnPasswordVisibilityToggled;
    }
}
