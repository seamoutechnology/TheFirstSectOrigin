using UnityEngine;
using UnityEngine.UI;
using GameClient.Managers;
using GameClient.Core.Interfaces;
using System.Collections.Generic;
using GameClient.Core;
using TMPro;
using DG.Tweening;
using GameClient.UI.Presenters;
using System;
using VContainer;

namespace GameClient.UI
{
    public class ZoneSelectPanel : MonoBehaviour, IZoneSelectView
    {
        [Header("Sidebar (All Tabs)")]
        public Transform tabRoot;
        public GameObject tabButtonPrefab; // Legacy (not used directly for Instantiate anymore, pool uses it)
        
        [Header("Tab Colors (Normal/Selected)")]
        public Color normalTabTextColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        public Color selectedTabTextColor = new Color(0.6f, 0.3f, 0f, 1f);

        [Header("Main Content (Servers)")]
        public Transform serverListRoot;
        public GameObject serverItemPrefab;
        public Sprite spriteServerGood;
        public Sprite spriteServerFull;
        public Sprite spriteServerHasChar;
        public UnityEngine.UI.ScrollRect scrollRect;

        [Header("Footer Actions")]
        public Button btnEnter;
        public Button btnCloseTopLeft;

        private Dictionary<string, Image> _tabImages = new Dictionary<string, Image>();
        private Dictionary<string, TMP_Text> _tabTexts = new Dictionary<string, TMP_Text>();
        private Dictionary<string, Text> _tabLegacyTexts = new Dictionary<string, Text>();

        private ZoneSelectPresenter _presenter;

        public event Action OnCloseRequested;
        public event Action<string, string> OnTabSelected;
        public event Action<ZoneData> OnZoneSelected;

        public static event Action<ZoneData> OnGlobalZoneSelected;

        public bool IsVisible => gameObject.activeSelf;

        [Inject]
        public void Construct(ZoneSelectPresenter presenter)
        {
            _presenter = presenter;
            _presenter.SetView(this);
        }

        private void Awake()
        {
            if (serverListRoot != null)
            {
                var fitter = serverListRoot.GetComponent<ContentSizeFitter>();
                if (fitter == null)
                {
                    fitter = serverListRoot.gameObject.AddComponent<ContentSizeFitter>();
                }
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        private void Start()
        {
            if (btnCloseTopLeft != null)
            {
                btnCloseTopLeft.onClick.RemoveAllListeners();
                btnCloseTopLeft.onClick.AddListener(() => OnCloseRequested?.Invoke());
            }

            if (tabButtonPrefab != null)
                ObjectPoolManager.Instance.RegisterPool("ZoneTab", tabButtonPrefab);
            
            if (serverItemPrefab != null)
                ObjectPoolManager.Instance.RegisterPool("ZoneItem", serverItemPrefab);
        }

        public void Setup(object data = null)
        {
            if (_presenter == null)
            {
                Debug.LogError("[ZoneSelectPanel] Presenter is null! Make sure to bind ZoneSelectPresenter in GameLifetimeScope.");
                return;
            }

            ClearMainContent();
            
            for (int i = tabRoot.childCount - 1; i >= 0; i--)
            {
                ObjectPoolManager.Instance.Release("ZoneTab", tabRoot.GetChild(i).gameObject);
            }
            
            _tabImages.Clear();
            _tabTexts.Clear();
            _tabLegacyTexts.Clear();

            _presenter.Initialize();
        }

        public void ShowLoading()
        {
        }

        public void ShowError(string message)
        {
            Debug.LogError(message);
        }

        public void BuildTabs(List<TabInfo> tabs, string defaultTabId)
        {
            foreach (var tab in tabs)
            {
                GameObject tabObj = ObjectPoolManager.Instance.Get("ZoneTab", tabRoot);
                tabObj.SetActive(true);
                
                Image bg = tabObj.GetComponent<Image>();
                TMP_Text txt = tabObj.GetComponentInChildren<TMP_Text>();
                Text legacyTxt = tabObj.GetComponentInChildren<Text>();
                
                string localizedTabName = tab.name;
                if (tab.id == "recent")
                {
                    string loc = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.UI_SYSTEM, "ui_zone_recent");
                    if (!string.IsNullOrEmpty(loc) && !loc.StartsWith("[")) localizedTabName = loc;
                }
                else if (tab.id == "my_chars")
                {
                    string loc = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.UI_SYSTEM, "ui_zone_my_chars");
                    if (!string.IsNullOrEmpty(loc) && !loc.StartsWith("[")) localizedTabName = loc;
                }
                else
                {
                    string locPrefix = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.UI_SYSTEM, "ui_zone_group_prefix");
                    if (!string.IsNullOrEmpty(locPrefix) && !locPrefix.StartsWith("["))
                    {
                        localizedTabName = tab.name.Replace("Cụm", locPrefix);
                    }
                }

                if (txt != null) 
                {
                    txt.text = localizedTabName;
                    txt.enableAutoSizing = true;
                    txt.fontSizeMin = 14;
                    txt.fontSizeMax = 36;
                    txt.lineSpacing = -20;
                }
                else if (legacyTxt != null) 
                {
                    legacyTxt.text = localizedTabName;
                    legacyTxt.resizeTextForBestFit = true;
                    legacyTxt.resizeTextMinSize = 14;
                    legacyTxt.resizeTextMaxSize = 36;
                }
                
                if (bg != null) 
                {
                    _tabImages[tab.id] = bg;
                }
                if (txt != null) 
                {
                    _tabTexts[tab.id] = txt;
                    txt.color = normalTabTextColor;
                }
                if (legacyTxt != null) 
                {
                    _tabLegacyTexts[tab.id] = legacyTxt;
                    legacyTxt.color = normalTabTextColor;
                }
                
                Button btn = tabObj.GetComponent<Button>();
                if (btn != null)
                {
                    string tabId = tab.id; 
                    string tabName = tab.name;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnTabSelected?.Invoke(tabId, tabName));
                }
            }
        }

        public void HighlightTab(string tabId)
        {

            foreach (var kv in _tabTexts)
            {
                if (kv.Value != null) kv.Value.color = (kv.Key == tabId) ? selectedTabTextColor : normalTabTextColor;
            }
            
            foreach (var kv in _tabLegacyTexts)
            {
                if (kv.Value != null) kv.Value.color = (kv.Key == tabId) ? selectedTabTextColor : normalTabTextColor;
            }
        }

        public void ClearMainContent()
        {
            for (int i = serverListRoot.childCount - 1; i >= 0; i--)
            {
                ObjectPoolManager.Instance.Release("ZoneItem", serverListRoot.GetChild(i).gameObject);
            }
        }

        public void RenderServers(List<ZoneData> zones)
        {
            foreach (var zone in zones)
            {
                GameObject item = ObjectPoolManager.Instance.Get("ZoneItem", serverListRoot);
                item.SetActive(true);

                UpdateServerItemUI(item, zone);
                
                Button btn = item.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = zone.is_online;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnZoneSelected?.Invoke(zone));
                }
            }

            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.content.anchoredPosition = new Vector2(scrollRect.content.anchoredPosition.x, 0);
            }
        }

        private void UpdateServerItemUI(GameObject item, ZoneData zone)
        {
            Image bg = item.GetComponent<Image>();
            if (bg != null)
            {
                if (zone.has_character && spriteServerHasChar != null)
                {
                    bg.sprite = spriteServerHasChar;
                }
                else if (!zone.is_online && spriteServerFull != null) 
                {
                    bg.sprite = spriteServerFull;
                }
                else if (spriteServerGood != null)
                {
                    bg.sprite = spriteServerGood;
                }
            }

            Transform serverNameTxt = item.transform.Find("ServerNameText");
            Transform dateTxt = item.transform.Find("DateText");
            Transform tagRecommend = item.transform.Find("TagRecommend");
            
            if (serverNameTxt != null)
            {
                var tmp = serverNameTxt.GetComponent<TMP_Text>();
                var leg = serverNameTxt.GetComponent<Text>();
                if (tmp != null) tmp.text = zone.name;
                else if (leg != null) leg.text = zone.name;
            }
            else
            {
                var tmp = item.GetComponentInChildren<TMP_Text>();
                var leg = item.GetComponentInChildren<Text>();
                if (tmp != null) tmp.text = zone.name;
                else if (leg != null) leg.text = zone.name;
            }

            if (dateTxt != null)
            {
                var tmp = dateTxt.GetComponent<TMP_Text>();
                var leg = dateTxt.GetComponent<Text>();
                string statusText = LocalizationManager.Instance.GetText(GameClient.Core.GameConstants.LocaleTable.UI_SYSTEM, "ui_zone_status_open");
                if (string.IsNullOrEmpty(statusText) || statusText.StartsWith("[")) statusText = "Đang mở";
                
                if (tmp != null) tmp.text = statusText; 
                else if (leg != null) leg.text = statusText;
            }

            if (tagRecommend != null)
            {
                tagRecommend.gameObject.SetActive(true);
            }
        }

        public static void NotifyZoneSelected(ZoneData zone)
        {
            OnGlobalZoneSelected?.Invoke(zone);
        }

        public void Show()
        {
            gameObject.SetActive(true);
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            
            cg.DOKill();
            cg.alpha = 1f;
        }

        public void Hide()
        {
            CanvasGroup cg = GetComponent<CanvasGroup>();
            if (cg != null) cg.DOKill();
            
            gameObject.SetActive(false);
            if (_presenter != null)
            {
                _presenter.Dispose();
            }
        }

        [System.Serializable]
        public class MetaResponse
        {
            public List<TabInfo> tabs;
        }

        [System.Serializable]
        public class TabInfo
        {
            public string id;
            public string name;
        }

        [System.Serializable]
        public class DataResponse
        {
            public List<ZoneData> zones;
        }

        [System.Serializable]
        public class ZoneReq
        {
            public string type;
            public string tab_id;
        }

        [System.Serializable]
        public class ZoneData
        {
            public int id;
            public string name;
            public string host;
            public int port;
            public bool is_online;
            public bool has_character;
            public string character_name;
            public int character_level;
            public string character_avatar;
        }
    }
}
