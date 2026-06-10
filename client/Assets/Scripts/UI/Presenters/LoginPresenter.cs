using System;
using UnityEngine;
using GameClient.Managers;
using GameClient.Core;
using VContainer;

namespace GameClient.UI.Presenters
{
    public class LoginPresenter
    {
        private ILoginView _view;
        private readonly UIManager _uiManager;
        private readonly LocalizationManager _localization;

        [Inject]
        public LoginPresenter(UIManager uiManager, LocalizationManager localization)
        {
            _uiManager = uiManager;
            _localization = localization;
        }

        public void SetView(ILoginView view)
        {
            _view = view;
        }

        public void Initialize(object data)
        {
            _view.SyncPasswordVisibility(false); // Default

            if (data is string username)
            {
                _view.SetUsername(username);
            }

            _view.OnLoginRequested += HandleLogin;
            _view.OnRegisterRequested += HandleRegister;
            _view.OnPasswordVisibilityToggled += HandlePasswordVisibilityToggled;
        }

        public void Dispose()
        {
            _view.OnLoginRequested -= HandleLogin;
            _view.OnRegisterRequested -= HandleRegister;
            _view.OnPasswordVisibilityToggled -= HandlePasswordVisibilityToggled;
        }

        private async void HandleLogin()
        {
            string acc = _view.GetUsername();
            string pass = _view.GetPassword();

            if (string.IsNullOrEmpty(acc) || string.IsNullOrEmpty(pass))
            {
                _view.ShowError(_localization.GetText(GameConstants.LocaleTable.UI_SYSTEM, "error_empty_fields") ?? "Vui lòng nhập đầy đủ thông tin.");
                return;
            }

            _view.SetLoginInteractable(false);

            try
            {
                var response = await AccountManager.Instance.LoginAsync(acc, pass);

                if (response != null && response.Code == 0 && !string.IsNullOrEmpty(response.Token))
                {
                    var uiMan = _uiManager != null ? _uiManager : UIManager.Instance;
                    if (uiMan != null)
                    {
                        uiMan.OpenPanel("EntryPanel",null , false);
                        uiMan.DestroyPanel("LoginPanel");
                        
                        var mb = _view as MonoBehaviour;
                        if (mb != null && mb.gameObject != null)
                        {
                            UnityEngine.Object.Destroy(mb.gameObject);
                        }
                    }
                }
                else
                {
                    string errorMsg = response?.MessageId ?? _localization.GetText(GameConstants.LocaleTable.UI_SYSTEM, "error_unknown");
                    _view.ShowError(errorMsg);
                    _view.SetLoginInteractable(true);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Login] Lỗi: {ex.Message}");
                _view.ShowError(_localization.GetText(GameConstants.LocaleTable.UI_SYSTEM, "error_connection_failed") ?? "Lỗi kết nối tới máy chủ.");
                _view.SetLoginInteractable(true);
            }
        }

        private void HandleRegister()
        {
            _uiManager.OpenPanel("RegisterPanel", null, false);
            _view.Hide();
        }

        private void HandlePasswordVisibilityToggled(bool isVisible)
        {
            _view.SetPasswordInputType(isVisible);
        }
    }
}
