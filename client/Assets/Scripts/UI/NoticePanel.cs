using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using GameClient.Core;
using GameClient.Core.Interfaces;
using GameClient.Managers;
using GameClient.UI.Presenters;
using System;
using VContainer;

namespace GameClient.UI
{
    public class NoticePanel : MonoBehaviour, INoticeView
    {
        [System.Serializable]
        public class NoticeData
        {
            public int id;
            public string type;
            public string title;
            public string content;
            public string start_at;
            public string end_at;
            public bool is_active;
        }

        [Header("UI Bindings")]
        public Button btnClose;
        
        [Header("Left Sidebar - Tabs")]
        public Transform tabContainer;
        public GameObject tabPrefab; // Thêm prefab cho Tab

        [Header("Right Content")]
        public TMP_Text txtTitle;
        public TMP_Text txtDate;
        
        [Header("Dynamic Content Blocks")]
        public Transform contentContainer;
        public GameObject textBlockPrefab; // Prefab chứa TMP_Text có ContentSizeFitter
        public GameObject imageBlockPrefab; // Prefab chứa RawImage + AspectRatioFitter
        
        [Header("Scroll View")]
        public UnityEngine.UI.ScrollRect scrollRect; // Kéo thả Scroll Rect vào đây

        private List<GameObject> _tabInstances = new List<GameObject>();
        private List<GameObject> _contentBlocks = new List<GameObject>();
        private NoticePresenter _presenter;

        public event Action OnCloseRequested;
        public event Action<NoticeData> OnTabSelected;

        public bool IsVisible => gameObject.activeSelf;

        [Inject]
        public void Construct(NoticePresenter presenter)
        {
            _presenter = presenter;
            _presenter.SetView(this);
        }

        private void Start()
        {
            if (btnClose != null)
            {
                btnClose.onClick.RemoveAllListeners();
                btnClose.onClick.AddListener(() => OnCloseRequested?.Invoke());
            }

            if (tabPrefab != null)
            {
                ObjectPoolManager.Instance.RegisterPool("NoticeTab", tabPrefab);
            }
            if (textBlockPrefab != null)
            {
                ObjectPoolManager.Instance.RegisterPool("NoticeText", textBlockPrefab);
            }
            if (imageBlockPrefab != null)
            {
                ObjectPoolManager.Instance.RegisterPool("NoticeImage", imageBlockPrefab);
            }
        }

        public void Setup(object data = null)
        {
            if (_presenter == null)
            {
                Debug.LogError("[NoticePanel] Presenter is null! Make sure to bind NoticePresenter in GameLifetimeScope.");
                return;
            }

            ClearTabs();
            _presenter.Initialize();
        }

        public void ShowLoading()
        {
            txtTitle.text = "";
            txtDate.text = "";
            ClearContentBlocks();
            
            string loadingText = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.UI_SYSTEM, "ui_notice_loading");
            if (string.IsNullOrEmpty(loadingText) || loadingText.StartsWith("[")) loadingText = "Đang tải thông báo...";
            AddTextBlock(loadingText);
        }

        public void ShowError(string message)
        {
            ClearContentBlocks();
            AddTextBlock(message);
        }

        public void ShowEmptyMessage()
        {
            string titleText = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.UI_SYSTEM, "ui_notice_title");
            if (string.IsNullOrEmpty(titleText) || titleText.StartsWith("[")) titleText = "Thông báo";
            
            string emptyText = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.UI_SYSTEM, "ui_notice_empty");
            if (string.IsNullOrEmpty(emptyText) || emptyText.StartsWith("[")) emptyText = "Hiện tại không có thông báo nào.";

            txtTitle.text = titleText;
            txtDate.text = "";
            ClearContentBlocks();
            AddTextBlock(emptyText);
        }

        public void BuildTabs(List<NoticeData> notices)
        {
            ClearTabs();

            for (int i = 0; i < notices.Count; i++)
            {
                var notice = notices[i];
                GameObject tabGo = ObjectPoolManager.Instance.Get("NoticeTab", tabContainer);
                tabGo.SetActive(true);
                
                TMP_Text txtTabName = tabGo.GetComponentInChildren<TMP_Text>();
                if (txtTabName != null)
                {
                    string typePrefix = GetTypePrefix(notice.type);
                    txtTabName.text = $"{typePrefix} {notice.title}";
                }

                Button btnTab = tabGo.GetComponent<Button>();
                if (btnTab != null)
                {
                    btnTab.onClick.RemoveAllListeners();
                    btnTab.onClick.AddListener(() => OnTabSelected?.Invoke(notice));
                }

                _tabInstances.Add(tabGo);
            }
        }

        public void DisplayNoticeDetails(string title, string date, string content)
        {
            txtTitle.text = title;
            txtDate.text = date;
            
            ClearContentBlocks();
            ParseAndBuildContent(content);
            
            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private void ParseAndBuildContent(string rawContent)
        {
            string[] blocks = rawContent.Split(new string[] { "[img]" }, StringSplitOptions.None);
            
            for (int i = 0; i < blocks.Length; i++)
            {
                if (i == 0)
                {
                    if (!string.IsNullOrWhiteSpace(blocks[i]))
                        AddTextBlock(blocks[i]);
                    continue;
                }

                int endIdx = blocks[i].IndexOf("[/img]");
                if (endIdx != -1)
                {
                    string imgUrl = blocks[i].Substring(0, endIdx);
                    if (!string.IsNullOrWhiteSpace(imgUrl))
                    {
                        // Decode các ký tự Unicode Escape phổ biến trong JSON truyền từ Server xuống
                        imgUrl = imgUrl.Replace("\\u0026", "&");
                        AddImageBlock(imgUrl);
                    }

                    string textAfterImg = blocks[i].Substring(endIdx + 6);
                    if (!string.IsNullOrWhiteSpace(textAfterImg))
                    {
                        AddTextBlock(textAfterImg);
                    }
                }
                else
                {
                    AddTextBlock("[img]" + blocks[i]);
                }
            }
        }

        private void AddTextBlock(string text)
        {
            if (contentContainer == null) return;
            GameObject txtObj = ObjectPoolManager.Instance.Get("NoticeText", contentContainer);
            txtObj.SetActive(true);
            var tmp = txtObj.GetComponentInChildren<TMP_Text>();
            if (tmp != null) tmp.text = text;
            _contentBlocks.Add(txtObj);
        }

        private void AddImageBlock(string url)
        {
            if (contentContainer == null) return;
            GameObject imgObj = ObjectPoolManager.Instance.Get("NoticeImage", contentContainer);
            imgObj.SetActive(true);
            
            var rawImage = imgObj.GetComponentInChildren<RawImage>();
            if (rawImage != null)
            {
                rawImage.texture = null; // Clear old texture
                rawImage.color = new Color(1,1,1,0); // Hide until loaded
                _ = LoadRemoteImage(url, rawImage);
            }
            _contentBlocks.Add(imgObj);
        }

        private async System.Threading.Tasks.Task LoadRemoteImage(string url, RawImage target)
        {
            try
            {
                using (var req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
                {
                    var op = req.SendWebRequest();
                    while (!op.isDone) await System.Threading.Tasks.Task.Yield();
                    
                    if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        var texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(req);
                        if (target != null && target.gameObject != null)
                        {
                            target.texture = texture;
                            target.color = Color.white;
                            
                            var aspectFitter = target.GetComponent<AspectRatioFitter>();
                            if (aspectFitter != null)
                            {
                                aspectFitter.aspectRatio = (float)texture.width / texture.height;
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError($"[NoticePanel] Lỗi tải ảnh: {url} - {req.error}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NoticePanel] Exception tải ảnh: {ex.Message}");
            }
        }

        private void ClearContentBlocks()
        {
            foreach (var b in _contentBlocks)
            {
                if (b != null)
                {
                    if (b.name.StartsWith("NoticeText"))
                        ObjectPoolManager.Instance.Release("NoticeText", b);
                    else
                        ObjectPoolManager.Instance.Release("NoticeImage", b);
                }
            }
            _contentBlocks.Clear();
        }

        private string GetTypePrefix(string type)
        {
            string prefix = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.UI_SYSTEM, $"ui_notice_type_{type.ToLower()}");
            
            switch (type)
            {
                case "MAINTENANCE": 
                    if (string.IsNullOrEmpty(prefix) || prefix.StartsWith("[")) prefix = "[Bảo Trì]";
                    return $"<color=red>{prefix}</color>";
                case "RULES": 
                    if (string.IsNullOrEmpty(prefix) || prefix.StartsWith("[")) prefix = "[Luật]";
                    return $"<color=yellow>{prefix}</color>";
                case "ACTIVITY": 
                    if (string.IsNullOrEmpty(prefix) || prefix.StartsWith("[")) prefix = "[Sự Kiện]";
                    return $"<color=green>{prefix}</color>";
                case "NEWS": 
                    if (string.IsNullOrEmpty(prefix) || prefix.StartsWith("[")) prefix = "[Tin Tức]";
                    return $"<color=#00BFFF>{prefix}</color>";
                default: 
                    return $"[{type}]";
            }
        }

        private void ClearTabs()
        {
            foreach (var t in _tabInstances)
            {
                if (t != null) 
                {
                    ObjectPoolManager.Instance.Release("NoticeTab", t);
                }
            }
            _tabInstances.Clear();
        }

        private void OnDestroy()
        {
            if (_presenter != null)
            {
                _presenter.Dispose();
            }
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() 
        {
            gameObject.SetActive(false);
            if (_presenter != null)
            {
                _presenter.Dispose(); // Dọn dẹp event khi đóng
            }
        }
    }
}
