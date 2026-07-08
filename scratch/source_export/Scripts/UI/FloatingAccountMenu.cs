using UnityEngine;
using UnityEngine.UI;
using GameClient.Managers;
using GameClient.Core;
using DG.Tweening;
using GameClient.Network;

namespace GameClient.UI
{
    public class FloatingAccountMenu : MonoBehaviour
    {
        [Header("Vùng chứa các nút phụ")]
        [Tooltip("Kéo Panel chứa các nút Nạp, Xem, Thoát vào đây. Lưu ý: Panel này nên làm Con (Child) của nút hiện tại.")]
        public GameObject subMenuContainer; 
        
        [Header("Các chức năng")]
        public Button btnAccountInfo; // Mở bảng AccountDashboardPanel (nếu cần)
        public Button btnTopUp;       // Nạp tiền
        public Button btnLogout;      // Đăng xuất

        private bool _isExpanded = false;

        private void Start()
        {
            if (subMenuContainer != null) subMenuContainer.SetActive(false);

            Button standardBtn = GetComponent<Button>();
            if (standardBtn != null) 
            {
                standardBtn.onClick.RemoveAllListeners();
                standardBtn.onClick.AddListener(ToggleMenu);
            }
            else
            {
                DraggableActionButton dragBtn = GetComponent<DraggableActionButton>();
                if (dragBtn != null)
                {
                    dragBtn.onClick.RemoveAllListeners();
                    dragBtn.onClick.AddListener(ToggleMenu);
                }
            }
            
            if (btnAccountInfo != null) 
            {
                btnAccountInfo.onClick.RemoveAllListeners();
                btnAccountInfo.onClick.AddListener(OnAccountInfoClicked);
            }

            if (btnTopUp != null) 
            {
                btnTopUp.onClick.RemoveAllListeners();
                btnTopUp.onClick.AddListener(OnTopUpClicked);
            }

            if (btnLogout != null) 
            {
                btnLogout.onClick.RemoveAllListeners();
                btnLogout.onClick.AddListener(OnLogoutClicked);
            }
        }

        public void ToggleMenu()
        {
            /*
             bool isLoggedIn = !string.IsNullOrEmpty(AccountManager.Instance.CurrentToken);
             if (!isLoggedIn)
             {
                 UIManager.Instance.OpenPanel("LoginPanel");
                 return;
             }
             */

            _isExpanded = !_isExpanded;
            
            if (subMenuContainer != null)
            {
                CanvasGroup cg = subMenuContainer.GetComponent<CanvasGroup>();
                if (cg == null) cg = subMenuContainer.AddComponent<CanvasGroup>();
                
                subMenuContainer.transform.DOKill();
                cg.DOKill();

                if (_isExpanded)
                {
                    UpdateMenuDirection();
                    subMenuContainer.SetActive(true);
                    
                    cg.alpha = 0f;
                    subMenuContainer.transform.localScale = new Vector3(0f, 1f, 1f);
                    
                    cg.DOFade(1f, 0.25f).SetUpdate(true);
                    subMenuContainer.transform.DOScaleX(1f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);

                    DraggableActionButton dragBtn = GetComponent<DraggableActionButton>();
                    if (dragBtn != null) 
                    {
                        dragBtn.KeepFullyOpaque = true;
                        CanvasGroup parentGroup = GetComponent<CanvasGroup>();
                        if (parentGroup != null) 
                        {
                            parentGroup.DOKill();
                            parentGroup.alpha = 1f;
                        }
                    }
                }
                else
                {
                    cg.DOFade(0f, 0.2f).SetUpdate(true);
                    subMenuContainer.transform.DOScaleX(0f, 0.2f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() => subMenuContainer.SetActive(false));
                    
                    DraggableActionButton dragBtn = GetComponent<DraggableActionButton>();
                    if (dragBtn != null)
                    {
                        dragBtn.KeepFullyOpaque = false;
                        CanvasGroup parentGroup = GetComponent<CanvasGroup>();
                        if (parentGroup != null)
                        {
                            parentGroup.DOFade(dragBtn.idleOpacity, 0.2f).SetUpdate(true);
                        }
                    }
                }
            }
        }

        private void UpdateMenuDirection()
        {
            if (subMenuContainer == null) return;

            RectTransform mainRect = GetComponent<RectTransform>();
            RectTransform subRect = subMenuContainer.GetComponent<RectTransform>();
            
            if (mainRect == null || subRect == null) return;

            Vector3[] corners = new Vector3[4];
            mainRect.GetWorldCorners(corners);
            float centerX = (corners[0].x + corners[2].x) / 2f;

            bool isRightSide = centerX > Screen.width / 2f;

            if (isRightSide)
            {
                subRect.pivot = new Vector2(1f, 0.5f); // Pivot ở mép phải
                subRect.localPosition = new Vector3(-mainRect.rect.width / 2f - 10f, 0f, 0f);
            }
            else
            {
                subRect.pivot = new Vector2(0f, 0.5f); // Pivot ở mép trái
                subRect.localPosition = new Vector3(mainRect.rect.width / 2f + 10f, 0f, 0f);
            }
        }

        private void OnAccountInfoClicked()
        {
            UIManager.Instance.OpenPanel("AccountDashboardPanel");
            ToggleMenu(); // Đóng menu nổi lại
        }

        private void OnTopUpClicked()
        {
            Debug.Log("[FloatingMenu] Nạp tiền...");
            string token = AccountManager.Instance.CurrentToken;
            if (string.IsNullOrEmpty(token))
            {
                UIManager.Instance.ShowMessage("Lỗi", "Bạn chưa đăng nhập!");
                return;
            }

            int zoneId = GameContext.CurrentServerId;
            if (zoneId <= 0)
            {
                zoneId = 1;
            }

            string baseUrl = NetworkManager.Instance != null ? NetworkManager.Instance.GetApiBaseUrl() : (GameSettings.Instance != null ? GameSettings.Instance.apiBaseUrl : "http://localhost:8080");
            string url = baseUrl.TrimEnd('/') + "/user/dashboard?token=" + System.Uri.EscapeDataString(token) + "&zone_id=" + zoneId;
            
            Debug.Log("[Dashboard] Mở cổng tu luyện ngoài từ FloatingMenu: " + url);
            Application.OpenURL(url);
            
            ToggleMenu(); // Đóng menu
        }

        private void OnLogoutClicked()
        {
            Debug.Log("[FloatingMenu] Đăng xuất...");
            AccountManager.Instance.Logout();
            UIManager.Instance.GoToLogin();
            ToggleMenu(); // Đóng menu
        }
    }
}
