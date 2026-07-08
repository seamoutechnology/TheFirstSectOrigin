using UnityEngine;
using UnityEngine.UI;
using GameClient.Network;
using GameClient.Core.Interfaces; // Sửa namespace chuẩn
using System.Threading.Tasks;
using GameClient.Core;
using TMPro;

namespace GameClient.UI
{
    public class AccountDashboardPanel : MonoBehaviour, IUIView
    {
        [Header("UI Binding")]
        public TMP_Text txtUserName;
        public TMP_Text txtAccountId;  // Thay thế Level bằng ID Tài Khoản
        public TMP_Text txtAccountRole; // Thay thế Tông môn bằng Quyền (User/VIP)
        public TMP_Text txtActiveZone; 
        public TMP_Text txtPlayTime;   
        
        public Button btnRedeem;
        public Button btnLogout;  // Nút đăng xuất
        public Button btnTopUp;   // Nút nạp tiền
        public Button btnClose;
        public Button btnCopyAccountId; // Nút copy ID

        private string _currentAccountId; // Lưu tạm ID để copy

        public bool IsVisible => gameObject.activeSelf; 

        public void Setup(object data)
        {
            if (btnClose != null)
            {
                btnClose.onClick.RemoveAllListeners();
                btnClose.onClick.AddListener(Hide);
            }

            if (btnLogout != null)
            {
                btnLogout.onClick.RemoveAllListeners();
                btnLogout.onClick.AddListener(OnLogoutClicked);
            }

            if (btnTopUp != null)
            {
                btnTopUp.onClick.RemoveAllListeners();
                btnTopUp.onClick.AddListener(OnTopUpClicked);
            }

            if (btnRedeem != null)
            {
                btnRedeem.onClick.RemoveAllListeners();
                btnRedeem.onClick.AddListener(OnRedeemClicked);
            }

            if (btnCopyAccountId != null)
            {
                btnCopyAccountId.onClick.RemoveAllListeners();
                btnCopyAccountId.onClick.AddListener(OnCopyAccountId);
            }

            _ = FetchUserData();
        }

        private async Task FetchUserData()
        {
            Debug.Log("[MiniApp] Đang fetch dữ liệu từ Web...");
            
            var profile = await NetworkManager.Instance.PostAsync<UserProfileData>("/api/profile", new {});

            if (profile != null)
            {
                string realUsername = Managers.AccountManager.Instance.CurrentUsername;
                if (string.IsNullOrEmpty(realUsername)) realUsername = profile.username; // Fallback
                
                if (txtUserName != null) txtUserName.text = realUsername;
                
                _currentAccountId = "1000" + UnityEngine.Random.Range(10, 99);
                if (txtAccountId != null) txtAccountId.text = Managers.LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.UI_SYSTEM, "ui_account_id", _currentAccountId);
                if (txtAccountRole != null) txtAccountRole.text = Managers.LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.UI_SYSTEM, "ui_account_role", "Người chơi");
                
                if (txtActiveZone != null) 
                {
                    string zoneName = string.IsNullOrEmpty(Managers.GameContext.CurrentServerName) 
                        ? Managers.LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.UI_SYSTEM, "ui_account_no_server") 
                        : Managers.GameContext.CurrentServerName;
                    txtActiveZone.text = Managers.LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.UI_SYSTEM, "ui_account_server", zoneName);
                }
                
                if (txtPlayTime != null)
                {
                    int playMinutes = Mathf.FloorToInt(Time.realtimeSinceStartup / 60f);
                    txtPlayTime.text = Managers.LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.UI_SYSTEM, "ui_account_playtime", playMinutes);
                }
            }
            else
            {
                if (txtUserName != null) txtUserName.text = Managers.LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.UI_SYSTEM, "ui_account_error");
            }
        }

        private void OnLogoutClicked()
        {
            Managers.AccountManager.Instance.Logout();
            UIManager.Instance.GoToLogin();
        }

        private void OnTopUpClicked()
        {
            OpenUserDashboard();
        }

        private void OnRedeemClicked()
        {
            OpenUserDashboard();
        }

        private void OpenUserDashboard()
        {
            string token = Managers.AccountManager.Instance.CurrentToken;
            if (string.IsNullOrEmpty(token))
            {
                UIManager.Instance.ShowMessage("Lỗi", "Bạn chưa đăng nhập!");
                return;
            }

            int zoneId = Managers.GameContext.CurrentServerId;
            if (zoneId <= 0)
            {
                zoneId = 1;
            }

            string baseUrl = NetworkManager.Instance != null ? NetworkManager.Instance.GetApiBaseUrl() : (GameSettings.Instance != null ? GameSettings.Instance.apiBaseUrl : "http://localhost:8080");
            string url = baseUrl.TrimEnd('/') + "/user/dashboard?token=" + System.Uri.EscapeDataString(token) + "&zone_id=" + zoneId;
            
            Debug.Log("[Dashboard] Mở cổng tu luyện ngoài: " + url);
            Application.OpenURL(url);
        }

        private void OnCopyAccountId()
        {
            if (!string.IsNullOrEmpty(_currentAccountId))
            {
                GUIUtility.systemCopyBuffer = _currentAccountId;
                string copiedMsg = Managers.LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.UI_SYSTEM, "ui_copied") ?? "Đã sao chép: ";
                UIManager.Instance.ShowMessage(Managers.LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_notice_toast"), copiedMsg + _currentAccountId);
            }
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);

        [System.Serializable]
        public class UserProfileData
        {
            public string username;
            public string email;
            public string role;
        }
    }
}
