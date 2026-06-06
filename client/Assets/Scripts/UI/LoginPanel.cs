using UnityEngine;
using TMPro;
using UnityEngine.UI;
using GameClient.Managers;
using GameClient.Core.Interfaces;
using GameClient.UI.Presenters;
using System;
using VContainer;

namespace GameClient.UI
{
    public class LoginPanel : MonoBehaviour, ILoginView
    {
        [Header("UI References")]
        public TMP_InputField inputAccount;
        public TMP_InputField inputPassword;
        public Button btnLogin;
        public Button btnRegister;
        public Toggle toggleShowPassword;
        public TMP_Text txtError;

        private LoginPresenter _presenter;

        public event Action OnLoginRequested;
        public event Action OnRegisterRequested;
        public event Action<bool> OnPasswordVisibilityToggled;

        public bool IsVisible => gameObject.activeSelf;

        [Inject]
        public void Construct(LoginPresenter presenter)
        {
            _presenter = presenter;
            _presenter.SetView(this);
        }

        private void Start()
        {
            if (btnLogin != null)
            {
                btnLogin.onClick.RemoveAllListeners();
                btnLogin.onClick.AddListener(() => OnLoginRequested?.Invoke());
            }

            if (btnRegister != null)
            {
                btnRegister.onClick.RemoveAllListeners();
                btnRegister.onClick.AddListener(() => OnRegisterRequested?.Invoke());
            }

            if (toggleShowPassword != null)
            {
                toggleShowPassword.onValueChanged.RemoveAllListeners();
                toggleShowPassword.onValueChanged.AddListener((val) => OnPasswordVisibilityToggled?.Invoke(val));
            }
        }

        public void Setup(object data = null)
        {
            if (_presenter == null)
            {
                Debug.LogError("[LoginPanel] Presenter is null! Make sure to bind LoginPresenter in GameLifetimeScope.");
                return;
            }

            _presenter.Initialize(data);
            
            if (txtError != null) txtError.gameObject.SetActive(false);
        }

        public string GetUsername() => inputAccount != null ? inputAccount.text.Trim() : "";
        public string GetPassword() => inputPassword != null ? inputPassword.text.Trim() : "";

        public void SetUsername(string username)
        {
            if (inputAccount != null) inputAccount.text = username;
        }

        public void ShowError(string msg)
        {
            if (txtError != null)
            {
                txtError.text = msg;
                txtError.gameObject.SetActive(true);
            }
            else
            {
                if (UIManager.Instance != null) UIManager.Instance.ShowMessage("Lỗi", msg);
            }
        }

        public void SetLoginInteractable(bool interactable)
        {
            if (btnLogin != null) btnLogin.interactable = interactable;
        }

        public void SyncPasswordVisibility(bool isVisible)
        {
            if (toggleShowPassword != null)
            {
                toggleShowPassword.SetIsOnWithoutNotify(isVisible);
                SetPasswordInputType(isVisible);
            }
        }

        public void SetPasswordInputType(bool isVisible)
        {
            if (inputPassword != null)
            {
                inputPassword.contentType = isVisible ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
                inputPassword.ForceLabelUpdate();
            }
        }

        private void HandleTabNavigation()
        {
            if (inputAccount != null && inputAccount.isFocused)
            {
                inputPassword?.Select();
            }
            else if (inputPassword != null && inputPassword.isFocused)
            {
                inputAccount?.Select();
            }
        }

        private void HandleSubmit()
        {
            if (inputAccount != null && inputAccount.isFocused)
            {
                inputPassword?.Select();
            }
            else
            {
                OnLoginRequested?.Invoke();
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            
            if (InputManager.Instance != null)
            {
                InputManager.Instance.RegisterTap("Login_Tab", UnityEngine.InputSystem.Key.Tab, HandleTabNavigation);
                InputManager.Instance.RegisterTap("Login_Enter", UnityEngine.InputSystem.Key.Enter, HandleSubmit);
                InputManager.Instance.RegisterTap("Login_NumpadEnter", UnityEngine.InputSystem.Key.NumpadEnter, HandleSubmit);
            }
        }

        public void Hide()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.Unregister("Login_Tab");
                InputManager.Instance.Unregister("Login_Enter");
                InputManager.Instance.Unregister("Login_NumpadEnter");
            }
            
            gameObject.SetActive(false);
            
            if (_presenter != null)
            {
                _presenter.Dispose();
            }
        }
    }
}
