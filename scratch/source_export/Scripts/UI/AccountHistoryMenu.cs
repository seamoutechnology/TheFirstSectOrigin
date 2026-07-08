using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using GameClient.Managers;

namespace GameClient.UI
{
    public class AccountHistoryMenu : MonoBehaviour
    {
        [Header("Menu Bindings")]
        [Tooltip("Cái nút Icon có hình tài khoản để bấm vào mở menu")]
        public Button btnToggleMenu;
        [Tooltip("Cái khung nền chứa danh sách tài khoản (để ẩn/hiện)")]
        public GameObject menuContainer;
        [Tooltip("Transform chứa các account item sinh ra")]
        public Transform contentRoot;
        [Tooltip("Prefab của 1 dòng tài khoản")]
        public GameObject accountItemPrefab; 

        [Header("Tương tác")]
        [Tooltip("Kéo LoginPanel vào đây để khi bấm chọn, tự nó điền User/Pass")]
        public LoginPanel loginPanel;

        private void Start()
        {
            if (menuContainer != null) menuContainer.SetActive(false);
            if (btnToggleMenu != null)
            {
                btnToggleMenu.onClick.RemoveAllListeners();
                btnToggleMenu.onClick.AddListener(ToggleMenu);
            }
        }

        public void ToggleMenu()
        {
            if (menuContainer == null) return;

            bool willShow = !menuContainer.activeSelf;
            menuContainer.SetActive(willShow);

            if (willShow)
            {
                RefreshList();
            }
        }

        private void RefreshList()
        {
            if (contentRoot == null || accountItemPrefab == null) return;

            foreach (Transform child in contentRoot)
            {
                if (child.gameObject != accountItemPrefab)
                {
                    Destroy(child.gameObject);
                }
            }

            var accounts = AccountManager.Instance.GetSavedAccounts();
            if (accounts == null || accounts.Count == 0) return;

            foreach (var acc in accounts)
            {
                GameObject itemObj = Instantiate(accountItemPrefab, contentRoot);
                itemObj.SetActive(true);

                var txtName = itemObj.GetComponentInChildren<TMP_Text>();
                if (txtName != null) txtName.text = acc.Username;

                var btnSelect = itemObj.GetComponent<Button>();
                if (btnSelect != null)
                {
                    btnSelect.onClick.AddListener(() => OnSelectAccount(acc));
                }
            }
        }

        private void OnSelectAccount(LocalAccountData acc)
        {
            if (loginPanel != null)
            {
                loginPanel.inputAccount.text = acc.Username;
                
                try
                {
                    string decryptedPass = GameClient.Utils.CryptoUtils.Decrypt(acc.EncryptedPassword);
                    loginPanel.inputPassword.text = decryptedPass;
                }
                catch
                {
                    loginPanel.inputPassword.text = ""; // Xóa trắng nếu lỗi giải mã
                }

                if (loginPanel.toggleShowPassword != null)
                {
                    loginPanel.SetPasswordInputType(loginPanel.toggleShowPassword.isOn);
                }
            }

            if (menuContainer != null) menuContainer.SetActive(false);
        }
    }
}
