using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using GameClient.Core;
using GameClient.Managers;

namespace GameClient.UI
{
    public class FloatingAgeMenu : MonoBehaviour
    {
        [Header("Vùng chứa các nút phụ")]
        [Tooltip("Kéo Panel chứa các nút (Thời gian chơi, Cảnh báo...) vào đây. Lưu ý: Panel này nên làm Con (Child) của nút 18+ hiện tại.")]
        public GameObject subMenuContainer; 
        
        [Header("Các chức năng")]
        public Button btnPlayTime;       // Xem thời gian chơi
        public Button btnHealthWarning;  // Xem cảnh báo sức khỏe 180 phút

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
            
            if (btnPlayTime != null) 
            {
                btnPlayTime.onClick.RemoveAllListeners();
                btnPlayTime.onClick.AddListener(OnPlayTimeClicked);
            }

            if (btnHealthWarning != null) 
            {
                btnHealthWarning.onClick.RemoveAllListeners();
                btnHealthWarning.onClick.AddListener(OnHealthWarningClicked);
            }
        }

        public void ToggleMenu()
        {
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
                subRect.pivot = new Vector2(1f, 0.5f);
                subRect.localPosition = new Vector3(-mainRect.rect.width / 2f - 10f, 0f, 0f);
            }
            else
            {
                subRect.pivot = new Vector2(0f, 0.5f);
                subRect.localPosition = new Vector3(mainRect.rect.width / 2f + 10f, 0f, 0f);
            }
        }

        private void OnPlayTimeClicked()
        {
            int playMinutes = PlayTimeManager.Instance.GetTodayPlayTimeMinutes();
            string localizedTitle = "Play Time";
            string localizedBody = $"You have played {playMinutes} minutes today."; // Basic fallback
            
            string locFormat = GameClient.Managers.LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, "ui_play_time_format");
            if (!string.IsNullOrEmpty(locFormat) && locFormat != "ui_play_time_format")
            {
                localizedBody = string.Format(locFormat, playMinutes);
            }

            UIManager.Instance.ShowMessage(localizedTitle, localizedBody);
            ToggleMenu(); 
        }

        private void OnHealthWarningClicked()
        {
            string localizedTitle = "18+ Warning";
            string localizedBody = GameClient.Managers.LocalizationManager.Instance.GetText(GameConstants.LocaleTable.UI_SYSTEM, GameConstants.Locales.WARN_18_PLUS);
            if (string.IsNullOrEmpty(localizedBody)) localizedBody = "Playing games for more than 180 minutes a day will negatively affect your health.";

            UIManager.Instance.ShowMessage(localizedTitle, localizedBody);
            ToggleMenu(); 
        }
    }
}
