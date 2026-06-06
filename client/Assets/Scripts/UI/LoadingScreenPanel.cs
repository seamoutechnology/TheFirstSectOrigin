using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameClient.UI.Core;
using GameClient.UI.Layout;
using System.Collections;
using DG.Tweening;

namespace GameClient.UI
{
    public class LoadingScreenPanel : BaseUIPanel
    {
        [SerializeField] private UILayoutBuilder layoutBuilder;
        [SerializeField] private string layoutName = "LoadingScreen";

        private RectTransform _progressBarFill;
        private TMP_Text _progressText;

        protected override void OnInit()
        {
            base.OnInit();
            
            UILayout layout = XMLLayoutManager.Instance.LoadLayout(layoutName);
            if (layout != null)
            {
                layoutBuilder.Build(layout, transform);
            }

            GameObject fillGo = layoutBuilder.GetElement("BarFill");
            if (fillGo != null) _progressBarFill = fillGo.GetComponent<RectTransform>();

            GameObject textGo = layoutBuilder.GetElement("TxtProgress");
            if (textGo != null) _progressText = textGo.GetComponent<TMP_Text>();
        }

        private System.Text.StringBuilder _sb = new System.Text.StringBuilder();

        public void UpdateProgress(float progress)
        {
            progress = Mathf.Clamp01(progress);
            
            if (_progressBarFill != null)
            {
                _progressBarFill.DOKill();
                _progressBarFill.DOAnchorMax(new Vector2(progress, _progressBarFill.anchorMax.y), 0.2f).SetEase(Ease.OutQuad);
            }

            if (_progressText != null)
            {
                _sb.Clear();
                _sb.Append((int)(progress * 100));
                _sb.Append("%");
                _progressText.text = _sb.ToString();
            }
        }

        public void StartLoading(IEnumerator loadingTask)
        {
            StartCoroutine(PerformLoading(loadingTask));
        }

        private IEnumerator PerformLoading(IEnumerator loadingTask)
        {
            UpdateProgress(0);
            yield return loadingTask;
            UpdateProgress(1);
            Hide();
        }
    }
}
