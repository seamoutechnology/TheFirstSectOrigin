using System;
using System.Text.RegularExpressions;
using UnityEngine;
using GameClient.Managers;
using GameClient.Core;
using GameClient.Network;
using GameClient.Network.Pb;
using VContainer;

namespace GameClient.UI.Presenters
{
    public class RegisterPresenter
    {
        private IRegisterView _view;
        private readonly UIManager _uiManager;
        private readonly LocalizationManager _localization;
        private readonly NetworkManager _network;

        [Inject]
        public RegisterPresenter(UIManager uiManager, LocalizationManager localization, NetworkManager network)
        {
            _uiManager = uiManager;
            _localization = localization;
            _network = network;
        }

        public void SetView(IRegisterView view)
        {
            _view = view;
        }

        public void Initialize()
        {
            _view.SyncPasswordVisibility(false); // Default

            _view.OnRegisterRequested += HandleRegister;
            _view.OnBackRequested += HandleBack;
            _view.OnPasswordVisibilityToggled += HandlePasswordVisibilityToggled;
        }

        public void Dispose()
        {
            _view.OnRegisterRequested -= HandleRegister;
            _view.OnBackRequested -= HandleBack;
            _view.OnPasswordVisibilityToggled -= HandlePasswordVisibilityToggled;
        }

        private async void HandleRegister()
        {
            string user = _view.GetUsername();
            string email = _view.GetEmail();
            string pass = _view.GetPassword();
            string confirm = _view.GetConfirmPassword();

            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass)) 
            {
                _view.ShowError(_localization.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_register_error_empty") ?? "Vui lòng nhập đầy đủ thông tin.");
                return;
            }

            if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) 
            {
                _view.ShowError(_localization.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_register_error_email_invalid") ?? "Email không hợp lệ.");
                return;
            }

            if (pass.Length < 6) 
            {
                _view.ShowError(_localization.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_register_error_pass_short") ?? "Mật khẩu quá ngắn.");
                return;
            }

            if (pass != confirm) 
            {
                _view.ShowError(_localization.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_register_error_pass_mismatch") ?? "Mật khẩu không khớp.");
                return;
            }

            _view.SetRegisterInteractable(false);

            try 
            {
                if (_network.AuthClient == null) _network.ConnectToDefaultGateway();

                var req = new RegisterRequest { Username = user, Email = email, Password = pass };
                var resp = await _network.AuthClient.RegisterAsync(req);

                if (resp.Code == 0) 
                {
                    _uiManager.ShowMessage(
                        _localization.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_success_title") ?? "Thành công",
                        _localization.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_register_success") ?? "Đăng ký thành công!"
                    );
                    _uiManager.OpenPanel("LoginPanel", user);
                    _view.Hide();
                } 
                else 
                {
                    string errorMsg = resp?.MessageId ?? _localization.GetText(GameConstants.LocaleTable.UI_SYSTEM, "error_unknown");
                    _view.ShowError(errorMsg);
                }
            } 
            catch (Exception e) 
            {
                Debug.LogError("[Register] Lỗi kết nối: " + e.Message);
                _view.ShowError(_localization.GetText(GameConstants.LocaleTable.UI_SYSTEM, "error_connection_failed") ?? "Lỗi kết nối máy chủ.");
            }
            finally 
            {
                _view.SetRegisterInteractable(true);
            }
        }

        private void HandleBack()
        {
            _uiManager.OpenPanel("LoginPanel");
            _view.Hide();
        }

        private void HandlePasswordVisibilityToggled(bool isVisible)
        {
            _view.SetPasswordInputType(isVisible);
        }
    }
}
