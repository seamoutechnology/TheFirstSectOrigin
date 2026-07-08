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
    public class RegisterPanel : MonoBehaviour, IRegisterView
    {
        [Header("UI Binding")]
        public TMP_InputField inputUser;
        public TMP_InputField inputEmail;
        public TMP_InputField inputPass;
        public TMP_InputField inputConfirm;
        public Button btnRegister;
        public Button btnBack;
        public Toggle toggleShowPassword;
        public TMP_Text txtError;

        private RegisterPresenter _presenter;

        public event Action OnRegisterRequested;
        public event Action OnBackRequested;
        public event Action<bool> OnPasswordVisibilityToggled;

        public bool IsVisible => gameObject.activeSelf;

        [Inject]
        public void Construct(RegisterPresenter presenter)
        {
            _presenter = presenter;
            _presenter.SetView(this);
        }

        private void Start()
        {
            if (btnRegister != null)
            {
                btnRegister.onClick.RemoveAllListeners();
                btnRegister.onClick.AddListener(() => OnRegisterRequested?.Invoke());
            }

            if (btnBack != null)
            {
                btnBack.onClick.RemoveAllListeners();
                btnBack.onClick.AddListener(() => OnBackRequested?.Invoke());
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
                Debug.LogError("[RegisterPanel] Presenter is null! Make sure to bind RegisterPresenter in GameLifetimeScope.");
                return;
            }

            _presenter.Initialize();
            
            if (txtError != null) txtError.gameObject.SetActive(false);
        }

        public string GetUsername() => inputUser != null ? inputUser.text.Trim() : "";
        public string GetEmail() => inputEmail != null ? inputEmail.text.Trim() : "";
        public string GetPassword() => inputPass != null ? inputPass.text.Trim() : "";
        public string GetConfirmPassword() => inputConfirm != null ? inputConfirm.text.Trim() : "";

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

        public void SetRegisterInteractable(bool interactable)
        {
            if (btnRegister != null) btnRegister.interactable = interactable;
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
            var type = isVisible ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;
            if (inputPass != null) 
            {
                inputPass.contentType = type;
                inputPass.ForceLabelUpdate();
            }
            if (inputConfirm != null) 
            {
                inputConfirm.contentType = type;
                inputConfirm.ForceLabelUpdate();
            }
        }

        private void HandleTabNavigation()
        {
            if (inputUser != null && inputUser.isFocused) inputEmail?.Select();
            else if (inputPass != null && inputPass.isFocused) inputConfirm?.Select();
            else if (inputConfirm != null && inputConfirm.isFocused) inputUser?.Select();
            else if (inputEmail != null && inputEmail.isFocused) inputPass?.Select();
        }

        private void HandleSubmit()
        {
            if (inputUser != null && inputUser.isFocused) inputEmail?.Select();
            else if (inputPass != null && inputPass.isFocused) inputConfirm?.Select();
            else if (inputEmail != null && inputEmail.isFocused) inputPass?.Select();
            else OnRegisterRequested?.Invoke();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            
            if (InputManager.Instance != null)
            {
                InputManager.Instance.RegisterTap("Register_Tab", UnityEngine.InputSystem.Key.Tab, HandleTabNavigation);
                InputManager.Instance.RegisterTap("Register_Enter", UnityEngine.InputSystem.Key.Enter, HandleSubmit);
                InputManager.Instance.RegisterTap("Register_NumpadEnter", UnityEngine.InputSystem.Key.NumpadEnter, HandleSubmit);
            }
        }

        public void Hide()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.Unregister("Register_Tab");
                InputManager.Instance.Unregister("Register_Enter");
                InputManager.Instance.Unregister("Register_NumpadEnter");
            }
            
            gameObject.SetActive(false);
            
            if (_presenter != null)
            {
                _presenter.Dispose();
            }
        }
    }
}
