using UnityEngine;
using GameClient.Core;
using GameClient.Managers;

namespace GameClient.UI
{
    public class ReconnectionOverlay : MonoBehaviour
    {
        private void Awake()
        {
            EventManager.Instance.AddListener("ON_SERVER_DISCONNECTED", OnDisconnected);
            EventManager.Instance.AddListener("ON_SERVER_RECONNECTED", OnReconnected);
            EventManager.Instance.AddListener("ON_SERVER_RECONNECT_FAILED", OnFailed);
            
            HideOverlay();
        }

        private void OnDestroy()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.RemoveListener("ON_SERVER_DISCONNECTED", OnDisconnected);
                EventManager.Instance.RemoveListener("ON_SERVER_RECONNECTED", OnReconnected);
                EventManager.Instance.RemoveListener("ON_SERVER_RECONNECT_FAILED", OnFailed);
            }
        }

        private void OnDisconnected(object data)
        {
            ShowOverlay("Đang kết nối lại...");
            if (ToastManager.Instance != null)
            {
                ToastManager.Instance.ShowBigToast("Mất kết nối máy chủ! Đang tiến hành kết nối lại...", 2f);
            }
        }

        private void OnReconnected(object data)
        {
            HideOverlay();
            if (ToastManager.Instance != null)
            {
                ToastManager.Instance.ShowBigToast("Đã kết nối lại thành công!", 1.5f);
            }
        }

        private void OnFailed(object data)
        {
            ShowOverlay("Kết nối thất bại. Vui lòng đăng nhập lại!");
            
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowMessage("Mất Kết Nối", "Kết nối tới máy chủ thất bại sau nhiều lần thử. Vui lòng đăng nhập lại!", () =>
                {
                    GoToLogin();
                });
            }
            else
            {
                Invoke(nameof(GoToLogin), 3f);
            }
        }

        private void ShowOverlay(string message)
        {
            Debug.Log($"<color=orange>[ReconnectionOverlay] SHOW: {message}</color>");
        }

        private void HideOverlay()
        {
            Debug.Log($"<color=orange>[ReconnectionOverlay] HIDE</color>");
        }

        private void GoToLogin()
        {
            HideOverlay();
            UIManager.Instance.GoToLogin();
        }
    }
}
