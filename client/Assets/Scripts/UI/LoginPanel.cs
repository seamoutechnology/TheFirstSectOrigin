using UnityEngine;
using TMPro;
using UnityEngine.UI;
using GameClient.Managers;
using GameClient.Core.Interfaces;
using GameClient.UI.Presenters;
using System;
using GameClient.Core;
using GameClient.Network;
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

        private bool _showDebugIP = false;
        private string _tempIP = "";

        private void OnGUI()
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD || UNITY_ANDROID
            // Vẽ nút cấu hình ở góc trên bên phải màn hình
            if (GUI.Button(new Rect(Screen.width - 160, 10, 150, 45), "Config Server IP"))
            {
                _showDebugIP = !_showDebugIP;
                _tempIP = PlayerPrefs.GetString("TFSO_SERVER_IP", GameSettings.Instance?.gatewayAddr ?? "127.0.0.1:50051");
            }

            if (_showDebugIP)
            {
                // Vẽ hộp thoại nhập liệu giữa màn hình
                Rect boxRect = new Rect(Screen.width / 2 - 150, Screen.height / 2 - 90, 300, 180);
                GUI.Box(boxRect, "Cấu hình IP Server (gRPC Gateway)");

                GUI.Label(new Rect(boxRect.x + 20, boxRect.y + 40, 260, 20), "Nhập IP và Port (Ví dụ: 10.0.2.2:50051):");
                _tempIP = GUI.TextField(new Rect(boxRect.x + 20, boxRect.y + 60, 260, 30), _tempIP);

                if (GUI.Button(new Rect(boxRect.x + 50, boxRect.y + 110, 90, 40), "Lưu & Kết nối"))
                {
                    PlayerPrefs.SetString("TFSO_SERVER_IP", _tempIP.Trim());
                    PlayerPrefs.Save();
                    _showDebugIP = false;
                    
                    // Thực hiện kết nối đến IP mới
                    if (NetworkManager.Instance != null)
                    {
                        NetworkManager.Instance.ConnectToGateway(_tempIP.Trim());
                    }
                }

                if (GUI.Button(new Rect(boxRect.x + 160, boxRect.y + 110, 90, 40), "Đóng"))
                {
                    _showDebugIP = false;
                }
            }
            #endif
        }
    }
}
