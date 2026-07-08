using UnityEngine;
using GameClient.Core;
using GameClient.Core.Interfaces;
using DG.Tweening;

namespace GameClient.UI.Core
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BaseUIPanel : BaseBehaviour, IUIView
    {
        protected CanvasGroup canvasGroup;

        public bool IsVisible => gameObject.activeSelf && canvasGroup.alpha > 0;

        protected override void OnInit()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
            canvasGroup.DOKill();
            canvasGroup.alpha = 0;
            canvasGroup.DOFade(1f, 0.2f).SetUpdate(true);
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            OnShow();
        }

        public virtual void Hide()
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, 0.2f).SetUpdate(true).OnComplete(() =>
            {
                gameObject.SetActive(false);
                OnHide();
            });
        }

        public virtual void Setup(object data = null)
        {
            // Mặc định không làm gì, các lớp con ghi đè để xử lý dữ liệu truyền vào
        }

        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
    }
}
