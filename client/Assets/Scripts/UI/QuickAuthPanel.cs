using UnityEngine;
using TMPro;
using UnityEngine.UI;
using GameClient.Network;
using GameClient.Core.Interfaces;
using GameClient.Managers;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameClient.Core;

namespace GameClient.UI
{
    public class QuickAuthPanel : MonoBehaviour, IUIView
    {
        [Header("UI Binding")]
        public TMP_Text txtWelcome;
        public Button btnEnterGame;
        public Button btnLogout;
        public GameObject loadingIcon; 

        private List<ZoneSelectPanel.ZoneData> _cachedZones;

        public bool IsVisible => gameObject.activeSelf;

        public void Setup(object data = null)
        {
            string defaultName = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_default_player_name");
            var lastAcc = AccountManager.Instance.GetLastAccount();
            string lastAccName = lastAcc != null && !string.IsNullOrEmpty(lastAcc.Username) ? lastAcc.Username : defaultName;
            txtWelcome.text = LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_quick_auth_welcome", lastAccName);

            btnEnterGame.interactable = false;
            if (loadingIcon != null) loadingIcon.SetActive(true);
            
            _ = ValidateAndFetchData();

            btnEnterGame.onClick.RemoveAllListeners();
            btnEnterGame.onClick.AddListener(OnEnterGameClicked);

            btnLogout.onClick.RemoveAllListeners();
            btnLogout.onClick.AddListener(OnLogoutClicked);
        }

        private async Task ValidateAndFetchData()
        {
            Debug.Log("[QuickAuth] Đang xác thực Token và tải danh sách máy chủ...");

            var profile = await NetworkManager.Instance.PostAsync<AccountDashboardPanel.UserProfileData>("/api/profile", new { });
            
            if (profile == null)
            {
                Debug.LogWarning("[QuickAuth] Phiên làm việc hết hạn hoặc Token không hợp lệ. Đang thử đăng nhập lại bằng mật khẩu đã lưu...");
                
                var lastAcc = AccountManager.Instance.GetLastAccount();
                if (lastAcc != null && !string.IsNullOrEmpty(lastAcc.EncryptedPassword))
                {
                    string decryptedPass = GameClient.Utils.CryptoUtils.Decrypt(lastAcc.EncryptedPassword);
                    if (!string.IsNullOrEmpty(decryptedPass))
                    {
                        var loginRes = await AccountManager.Instance.LoginAsync(lastAcc.Username, decryptedPass);
                        if (loginRes != null && loginRes.Code == 0)
                        {
                            profile = await NetworkManager.Instance.PostAsync<AccountDashboardPanel.UserProfileData>("/api/profile", new { });
                        }
                    }
                }

                if (profile == null)
                {
                    Debug.LogWarning("[QuickAuth] Tự động đăng nhập thất bại. Yêu cầu nhập lại mật khẩu.");
                    UIManager.Instance.OpenPanel("LoginPanel");
                    this.Hide();
                    return;
                }
            }

            Debug.Log($"<color=green>[QuickAuth] Xác thực thành công! Tài khoản: {profile.username}, Quyền: {profile.role}</color>");

            var req = new ZoneSelectPanel.ZoneReq { type = "data", tab_id = "recent" };
            var dataRes = await NetworkManager.Instance.PostAsync<ZoneSelectPanel.DataResponse>("/api/zones", req);
            ZoneSelectPanel.ZoneData defaultZone = null;
            if (dataRes != null && dataRes.zones != null && dataRes.zones.Count > 0)
            {
                defaultZone = dataRes.zones[0];
            }
            
            btnEnterGame.interactable = true;
            if (loadingIcon != null) loadingIcon.SetActive(false);
        }

        private void OnEnterGameClicked()
        {
            UIManager.Instance.OpenPanel("EntryPanel", null, false);
            this.Hide();
        }

        private void OnLogoutClicked()
        {
            EventManager.Instance.Emit(GameEvents.ON_LOGOUT);

            AccountManager.Instance.Logout();
            
            UIManager.Instance.OpenPanel("LoginPanel");
            this.Hide();
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
