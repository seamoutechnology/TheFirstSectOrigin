using System.Collections.Generic;
using System.Threading.Tasks;
using GameClient.Core;
using UnityEngine;
using DG.Tweening;
using GameClient.Core.Interfaces;
using GameClient.UI;
using VContainer;
using VContainer.Unity;

namespace GameClient
{
    public class UIManager : Singleton<UIManager>
    {
        [SerializeField] private Transform canvasRoot; // Điểm thả UI trên Canvas
        
        [Inject] private IObjectResolver _resolver; // VContainer Resolver
        
        private readonly Dictionary<string, IUIView> _cachedPanels = new();
        private readonly Dictionary<string, Task<IUIView>> _loadingPanels = new();

        protected override void Awake()
        {
            base.Awake();
            
            if (canvasRoot != null && canvasRoot.parent == null)
            {
                DontDestroyOnLoad(canvasRoot.gameObject);
            }

            GameObject overlay = GameObject.Find("OverlayUI");
            if (overlay == null)
            {
                var overlayPrefab = Resources.Load<GameObject>("Prefabs/UI/OverlayUI");
                if (overlayPrefab != null)
                {
                    overlay = Instantiate(overlayPrefab);
                    overlay.name = "OverlayUI";
                    Debug.Log("[UIManager] Tự động load và khởi tạo OverlayUI từ Resources.");
                }
            }

            if (overlay != null)
            {
                DontDestroyOnLoad(overlay);
            }

            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureEventSystem();
        }

        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            EnsureEventSystem();
        }

        public void ClearAllPanels(params string[] excludeKeys)
        {
            var keysToRemove = new System.Collections.Generic.List<string>();
            foreach (var kvp in _cachedPanels)
            {
                if (excludeKeys != null && System.Array.IndexOf(excludeKeys, kvp.Key) >= 0)
                    continue; // Giữ lại panel này

                var mb = kvp.Value as MonoBehaviour;
                if (mb != null && mb.gameObject != null)
                {
                    ResourceManager.Instance.ReleaseInstance(mb.gameObject);
                }
                keysToRemove.Add(kvp.Key);
            }
            
            foreach (var k in keysToRemove)
            {
                _cachedPanels.Remove(k);
            }
            
        }

        public void OpenPanel(string addressableKey, object data = null, bool isLoadByPlatform = true)
        {
            EnsureEventSystem();
            _ = OpenPanelAsync(addressableKey, data, isLoadByPlatform);
        }

        private void EnsureEventSystem()
        {
            var eventSystems = Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsSortMode.None);
            if (eventSystems.Length > 1)
            {
                Debug.LogWarning($"[UIManager] Tìm thấy {eventSystems.Length} EventSystem trong scene. Tiến hành giữ lại cái đầu tiên và xoá các bản trùng lặp.");
                for (int i = 1; i < eventSystems.Length; i++)
                {
                    Destroy(eventSystems[i].gameObject);
                }
            }

            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem == null && eventSystems.Length > 0)
            {
                eventSystem = eventSystems[0];
            }

            if (eventSystem == null)
            {
                GameObject go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<UnityEngine.EventSystems.EventSystem>();
                go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                Debug.Log("[UIManager] Tự động tạo EventSystem vì không tìm thấy.");
            }
            else
            {
                var legacyModule = eventSystem.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (legacyModule != null)
                {
                    Destroy(legacyModule);
                }
                
                var newModule = eventSystem.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                if (newModule == null)
                {
                    eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    Debug.Log("[UIManager] Đã cấu hình InputSystemUIInputModule cho EventSystem.");
                }
            }
        }

        private async Task<bool> TryLoadConfirmPanelPrefabAsync(
            string title, string content, string acceptText, string denyText, 
            System.Action onAccept, System.Action onDeny)
        {
            GameObject go = null;
            try
            {
                // 1. Thử tải bằng Addressables trước (vì prefab nằm ngoài thư mục Resources)
                go = await ResourceManager.Instance.InstantiateAsync("UI_ConfirmPanel", canvasRoot);
            }
            catch (System.Exception) {}

            // 2. Dự phòng: Thử tải bằng Resources truyền thống
            if (go == null)
            {
                var confirmPrefab = Resources.Load<GameObject>("Prefabs/UI/UI_ConfirmPanel");
                if (confirmPrefab != null)
                {
                    go = Instantiate(confirmPrefab, canvasRoot);
                }
            }

            if (go != null)
            {
                go.transform.SetAsLastSibling();
                var script = go.GetComponent<UI_ConfirmPanel>();
                if (script != null)
                {
                    script.Setup(null);
                    script.SetupDialog(title, content, acceptText, denyText, onAccept, onDeny);
                    script.Show();
                    return true;
                }
                else
                {
                    Destroy(go);
                }
            }
            return false;
        }

        public void ShowMessage(string title, string content, System.Action onConfirm = null)
        {
            Debug.Log($"[POPUP] {title}: {content}");
            
            if (canvasRoot == null)
            {
                if (onConfirm != null) onConfirm.Invoke();
                return;
            }

            _ = ShowMessageAsync(title, content, onConfirm);
        }

        private async Task ShowMessageAsync(string title, string content, System.Action onConfirm)
        {
            bool success = await TryLoadConfirmPanelPrefabAsync(title, content, "OK", "", onConfirm, null);
            if (success) return;

            var root = new GameObject("UI_MessageDialogPopup");
            root.transform.SetParent(canvasRoot, false);
            root.transform.SetAsLastSibling();

            var canvasGroup = root.AddComponent<CanvasGroup>();

            var overlayGo = new GameObject("Overlay", typeof(UnityEngine.UI.Image));
            overlayGo.transform.SetParent(root.transform, false);
            var overlayImg = overlayGo.GetComponent<UnityEngine.UI.Image>();
            overlayImg.color = new Color(0, 0, 0, 0.65f);
            var overlayRect = overlayGo.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            var panelGo = new GameObject("Panel", typeof(UnityEngine.UI.Image));
            panelGo.transform.SetParent(root.transform, false);
            var panelImg = panelGo.GetComponent<UnityEngine.UI.Image>();
            panelImg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(400, 220);
            panelRect.anchoredPosition = Vector2.zero;

            var outline = panelGo.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0.3f, 0.3f, 0.4f, 0.5f);
            outline.effectDistance = new Vector2(1, -1);

            var titleGo = new GameObject("Title", typeof(TMPro.TextMeshProUGUI));
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleText = titleGo.GetComponent<TMPro.TextMeshProUGUI>();
            titleText.text = title;
            titleText.fontSize = 20;
            titleText.alignment = TMPro.TextAlignmentOptions.Center;
            titleText.color = new Color(0.9f, 0.8f, 0.6f);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.75f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(10, 0);
            titleRect.offsetMax = new Vector2(-10, 0);

            var contentGo = new GameObject("Content", typeof(TMPro.TextMeshProUGUI));
            contentGo.transform.SetParent(panelGo.transform, false);
            var contentText = contentGo.GetComponent<TMPro.TextMeshProUGUI>();
            contentText.text = content;
            contentText.fontSize = 16;
            contentText.alignment = TMPro.TextAlignmentOptions.Center;
            contentText.color = Color.white;
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0.3f);
            contentRect.anchorMax = new Vector2(1, 0.75f);
            contentRect.offsetMin = new Vector2(20, 0);
            contentRect.offsetMax = new Vector2(-20, 0);

            // Tận dụng prefab Button có sẵn trong Resources
            var btnPrefab = Resources.Load<GameObject>("Prefabs/UI/Component/Button");
            GameObject confirmBtnGo;

            if (btnPrefab != null)
            {
                confirmBtnGo = Instantiate(btnPrefab, panelGo.transform);
                confirmBtnGo.name = "ConfirmButton";
            }
            else
            {
                confirmBtnGo = new GameObject("ConfirmButton", typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                confirmBtnGo.transform.SetParent(panelGo.transform, false);
                var confirmBtnImg = confirmBtnGo.GetComponent<UnityEngine.UI.Image>();
                confirmBtnImg.color = new Color(0.2f, 0.5f, 0.8f);
            }

            var confirmBtnRect = confirmBtnGo.GetComponent<RectTransform>();
            confirmBtnRect.anchorMin = new Vector2(0.3f, 0.08f);
            confirmBtnRect.anchorMax = new Vector2(0.7f, 0.28f);
            confirmBtnRect.offsetMin = Vector2.zero;
            confirmBtnRect.offsetMax = Vector2.zero;

            var confirmTextComp = confirmBtnGo.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (confirmTextComp == null)
            {
                var confirmTextGo = new GameObject("Text", typeof(TMPro.TextMeshProUGUI));
                confirmTextGo.transform.SetParent(confirmBtnGo.transform, false);
                confirmTextComp = confirmTextGo.GetComponent<TMPro.TextMeshProUGUI>();
                var confirmTextRect = confirmTextGo.GetComponent<RectTransform>();
                confirmTextRect.anchorMin = Vector2.zero;
                confirmTextRect.anchorMax = Vector2.one;
                confirmTextRect.sizeDelta = Vector2.zero;
            }
            confirmTextComp.text = "OK";
            confirmTextComp.fontSize = 14;
            confirmTextComp.alignment = TMPro.TextAlignmentOptions.Center;
            confirmTextComp.color = Color.white;

            var confirmBtn = confirmBtnGo.GetComponent<UnityEngine.UI.Button>();
            confirmBtn.onClick.AddListener(() => {
                Destroy(root);
                onConfirm?.Invoke();
            });

            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, 0.2f).SetUpdate(true);
        }

        public void ShowConfirmDialog(
            string titleKey, 
            string contentFormat, 
            string contentArg, 
            string acceptKey, 
            string denyKey, 
            System.Action onAccept, 
            System.Action onDeny = null)
        {
            if (canvasRoot == null)
            {
                onAccept?.Invoke();
                return;
            }

            _ = ShowConfirmDialogAsync(titleKey, contentFormat, contentArg, acceptKey, denyKey, onAccept, onDeny);
        }

        private async Task ShowConfirmDialogAsync(
            string titleKey, 
            string contentFormat, 
            string contentArg, 
            string acceptKey, 
            string denyKey, 
            System.Action onAccept, 
            System.Action onDeny = null)
        {
            string title = Managers.LocalizationManager.Instance.GetText(titleKey);
            if (string.IsNullOrEmpty(title) || title.StartsWith("[")) title = titleKey;

            string rawContent = Managers.LocalizationManager.Instance.GetText(contentFormat);
            if (string.IsNullOrEmpty(rawContent) || rawContent.StartsWith("[")) rawContent = contentFormat;

            string content = string.IsNullOrEmpty(contentArg) ? rawContent : string.Format(rawContent, contentArg);

            string acceptText = Managers.LocalizationManager.Instance.GetText(acceptKey);
            if (string.IsNullOrEmpty(acceptText) || acceptText.StartsWith("[")) acceptText = "Đồng Ý";

            string denyText = Managers.LocalizationManager.Instance.GetText(denyKey);
            if (string.IsNullOrEmpty(denyText) || denyText.StartsWith("[")) denyText = "Hủy";

            bool success = await TryLoadConfirmPanelPrefabAsync(title, content, acceptText, denyText, onAccept, onDeny);
            if (success) return;

            // 2. Dự phòng tạo động
            var root = new GameObject("UI_ConfirmDialogPopup");
            root.transform.SetParent(canvasRoot, false);
            root.transform.SetAsLastSibling();

            var canvasGroup = root.AddComponent<CanvasGroup>();

            var overlayGo = new GameObject("Overlay", typeof(UnityEngine.UI.Image));
            overlayGo.transform.SetParent(root.transform, false);
            var overlayImg = overlayGo.GetComponent<UnityEngine.UI.Image>();
            overlayImg.color = new Color(0, 0, 0, 0.65f);
            var overlayRect = overlayGo.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            var panelGo = new GameObject("Panel", typeof(UnityEngine.UI.Image));
            panelGo.transform.SetParent(root.transform, false);
            var panelImg = panelGo.GetComponent<UnityEngine.UI.Image>();
            panelImg.color = new Color(0.12f, 0.12f, 0.16f, 0.95f);
            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(400, 220);
            panelRect.anchoredPosition = Vector2.zero;

            var outline = panelGo.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0.3f, 0.3f, 0.4f, 0.5f);
            outline.effectDistance = new Vector2(1, -1);

            var titleGo = new GameObject("Title", typeof(TMPro.TextMeshProUGUI));
            titleGo.transform.SetParent(panelGo.transform, false);
            var titleText = titleGo.GetComponent<TMPro.TextMeshProUGUI>();
            titleText.text = title;
            titleText.fontSize = 20;
            titleText.alignment = TMPro.TextAlignmentOptions.Center;
            titleText.color = new Color(0.9f, 0.8f, 0.6f);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.75f);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(10, 0);
            titleRect.offsetMax = new Vector2(-10, 0);

            var contentGo = new GameObject("Content", typeof(TMPro.TextMeshProUGUI));
            contentGo.transform.SetParent(panelGo.transform, false);
            var contentText = contentGo.GetComponent<TMPro.TextMeshProUGUI>();
            contentText.text = content;
            contentText.fontSize = 16;
            contentText.alignment = TMPro.TextAlignmentOptions.Center;
            contentText.color = Color.white;
            var contentRect = contentGo.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0.3f);
            contentRect.anchorMax = new Vector2(1, 0.75f);
            contentRect.offsetMin = new Vector2(20, 0);
            contentRect.offsetMax = new Vector2(-20, 0);

            // Tận dụng prefab Button có sẵn trong Resources
            var btnPrefab = Resources.Load<GameObject>("Prefabs/UI/Component/Button");
            GameObject denyBtnGo;
            GameObject acceptBtnGo;

            if (btnPrefab != null)
            {
                denyBtnGo = Instantiate(btnPrefab, panelGo.transform);
                denyBtnGo.name = "DenyButton";
                
                acceptBtnGo = Instantiate(btnPrefab, panelGo.transform);
                acceptBtnGo.name = "AcceptButton";
            }
            else
            {
                denyBtnGo = new GameObject("DenyButton", typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                denyBtnGo.transform.SetParent(panelGo.transform, false);
                var denyBtnImg = denyBtnGo.GetComponent<UnityEngine.UI.Image>();
                denyBtnImg.color = new Color(0.35f, 0.35f, 0.38f);
                
                acceptBtnGo = new GameObject("AcceptButton", typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
                acceptBtnGo.transform.SetParent(panelGo.transform, false);
                var acceptBtnImg = acceptBtnGo.GetComponent<UnityEngine.UI.Image>();
                acceptBtnImg.color = new Color(0.2f, 0.5f, 0.8f);
            }

            var denyBtnRect = denyBtnGo.GetComponent<RectTransform>();
            denyBtnRect.anchorMin = new Vector2(0.1f, 0.08f);
            denyBtnRect.anchorMax = new Vector2(0.45f, 0.28f);
            denyBtnRect.offsetMin = Vector2.zero;
            denyBtnRect.offsetMax = Vector2.zero;

            var denyTextComp = denyBtnGo.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (denyTextComp == null)
            {
                var denyTextGo = new GameObject("Text", typeof(TMPro.TextMeshProUGUI));
                denyTextGo.transform.SetParent(denyBtnGo.transform, false);
                denyTextComp = denyTextGo.GetComponent<TMPro.TextMeshProUGUI>();
                var denyTextRect = denyTextGo.GetComponent<RectTransform>();
                denyTextRect.anchorMin = Vector2.zero;
                denyTextRect.anchorMax = Vector2.one;
                denyTextRect.sizeDelta = Vector2.zero;
            }
            denyTextComp.text = denyText;
            denyTextComp.fontSize = 14;
            denyTextComp.alignment = TMPro.TextAlignmentOptions.Center;
            denyTextComp.color = Color.white;

            var acceptBtnRect = acceptBtnGo.GetComponent<RectTransform>();
            acceptBtnRect.anchorMin = new Vector2(0.55f, 0.08f);
            acceptBtnRect.anchorMax = new Vector2(0.9f, 0.28f);
            acceptBtnRect.offsetMin = Vector2.zero;
            acceptBtnRect.offsetMax = Vector2.zero;

            var acceptTextComp = acceptBtnGo.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (acceptTextComp == null)
            {
                var acceptTextGo = new GameObject("Text", typeof(TMPro.TextMeshProUGUI));
                acceptTextGo.transform.SetParent(acceptBtnGo.transform, false);
                acceptTextComp = acceptTextGo.GetComponent<TMPro.TextMeshProUGUI>();
                var acceptTextRect = acceptTextGo.GetComponent<RectTransform>();
                acceptTextRect.anchorMin = Vector2.zero;
                acceptTextRect.anchorMax = Vector2.one;
                acceptTextRect.sizeDelta = Vector2.zero;
            }
            acceptTextComp.text = acceptText;
            acceptTextComp.fontSize = 14;
            acceptTextComp.alignment = TMPro.TextAlignmentOptions.Center;
            acceptTextComp.color = Color.white;

            var denyBtn = denyBtnGo.GetComponent<UnityEngine.UI.Button>();
            denyBtn.onClick.AddListener(() => {
                Destroy(root);
                onDeny?.Invoke();
            });

            var acceptBtn = acceptBtnGo.GetComponent<UnityEngine.UI.Button>();
            acceptBtn.onClick.AddListener(() => {
                Destroy(root);
                onAccept?.Invoke();
            });

            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, 0.2f).SetUpdate(true);
        }

        public async Task<IUIView> OpenPanelAsync(string addressableKey, object data = null, bool isLoadByPlatform = true)
        {
            if (canvasRoot == null)
            {
                var canvas = GameObject.FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    canvasRoot = canvas.transform;
                    Debug.Log($"[UIManager] Auto-assigned canvasRoot to Canvas: {canvas.name}");
                }
                else
                {
                    var canvasGo = new GameObject("UICanvasFallback", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
                    canvasRoot = canvasGo.transform;
                    var c = canvasGo.GetComponent<Canvas>();
                    c.renderMode = RenderMode.ScreenSpaceOverlay;
                    Debug.LogWarning("[UIManager] canvasRoot is null and no Canvas found in scene! Created fallback UICanvasFallback.");
                }
            }

            try
            {
                if (_cachedPanels.TryGetValue(addressableKey, out var existingPanel))
                {
                    var mb = existingPanel as MonoBehaviour;
                    if (mb != null && mb.gameObject != null)
                    {
                        UISoundTrigger.AddToAll(mb.gameObject); 
                        existingPanel.Setup(data);
                        existingPanel.Show();
                        return existingPanel;
                    }
                    else
                    {
                        _cachedPanels.Remove(addressableKey);
                    }
                }

                if (_loadingPanels.TryGetValue(addressableKey, out var loadingTask))
                {
                    var loadedView = await loadingTask;
                    if (loadedView != null)
                    {
                        loadedView.Setup(data);
                        loadedView.Show();
                    }
                    return loadedView;
                }

                var tcs = new TaskCompletionSource<IUIView>();
                _loadingPanels[addressableKey] = tcs.Task;

                GameObject uiInstance = null;
                
                if (isLoadByPlatform)
                {
                    string platformSuffix = Application.isMobilePlatform ? "_Mobile" : "_PC";
                    string platformKey = addressableKey + platformSuffix;

                    try
                    {
                        uiInstance = await ResourceManager.Instance.InstantiateAsync(platformKey, canvasRoot);
                    }
                    catch (System.Exception)
                    {
                        Debug.LogWarning($"[UIManager] Không tải được panel theo platform key '{platformKey}'. Đang thử tải lại key gốc '{addressableKey}'...");
                    }
                }
                
                if (uiInstance == null)
                {
                    try 
                    {
                        uiInstance = await ResourceManager.Instance.InstantiateAsync(addressableKey, canvasRoot);
                    } 
                    catch (System.Exception ex)
                    {
                        _loadingPanels.Remove(addressableKey);
                        throw ex;
                    }
                }

                // Chống race-condition khi người chơi đã logout/GoToLogin trong lúc tải panel
                if (!_loadingPanels.ContainsKey(addressableKey))
                {
                    if (uiInstance != null) Destroy(uiInstance);
                    return null;
                }
                
                if (uiInstance != null)
                {
                    if (uiInstance.transform.parent == null && canvasRoot != null)
                    {
                        uiInstance.transform.SetParent(canvasRoot, false);
                        Debug.Log($"[UIManager] Ép {addressableKey} vào cha: {canvasRoot.name}");
                    }
                    else if (canvasRoot == null)
                    {
                        Debug.LogError("[UIManager] LỖI: canvasRoot bị NULL! Kiểm tra Inspector của UIManager.");
                    }
                }
                
                if (uiInstance == null)
                {
                    Debug.LogError($"[UIManager] Không tìm thấy Addressable key: {addressableKey}");
                    return null;
                }

                if (_resolver == null)
                {
                    _resolver = GameClient.Core.DI.GameLifetimeScope.GlobalResolver;
                }

                if (_resolver != null)
                {
                    _resolver.InjectGameObject(uiInstance);
                }
                else
                {
                    Debug.LogError("[UIManager] _resolver IS NULL! VContainer injection will not work for " + addressableKey);
                }

                UISoundTrigger.AddToAll(uiInstance);

                var panel = uiInstance.GetComponent<IUIView>();
                if (panel == null)
                {
                    Debug.LogError($"[UIManager] UI Prefab {addressableKey} không cài đặt IUIView (BaseUIPanel)");
                    return null;
                }

                _cachedPanels[addressableKey] = panel;
                
                panel.Setup(data);
                panel.Show();

                tcs.SetResult(panel);
                _loadingPanels.Remove(addressableKey);

                return panel;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[UIManager] LỖI NGẦM TRONG KHI MỞ PANEL {addressableKey}: {ex.Message}\n{ex.StackTrace}");
                if (_loadingPanels.ContainsKey(addressableKey)) 
                {
                    _loadingPanels.Remove(addressableKey);
                }
                return null;
            }
        }

        public IUIView GetPanel(string addressableKey)
        {
            if (_cachedPanels.TryGetValue(addressableKey, out var panel))
            {
                return panel;
            }
            return null;
        }

        public void ClosePanel(string addressableKey)
        {
            if (_cachedPanels.TryGetValue(addressableKey, out var panel))
            {
                panel.Hide();
            }
        }

        public void DestroyPanel(string addressableKey)
        {
            if (_cachedPanels.TryGetValue(addressableKey, out var panel))
            {
                var mb = panel as MonoBehaviour;
                if (mb != null)
                {
                    ResourceManager.Instance.ReleaseInstance(mb.gameObject);
                }
                _cachedPanels.Remove(addressableKey);
            }
        }
        
        public void SetCanvasRoot(Transform root) 
        {
            this.canvasRoot = root;
        }
        
        public void GoToLogin()
        {
            // Xoá danh sách loading để huỷ các tác vụ tải panel bất đồng bộ đang chờ
            _loadingPanels.Clear();

            foreach (var panel in _cachedPanels.Values)
            {
                var mb = panel as MonoBehaviour;
                if (mb != null && mb.gameObject != null) 
                {
                    panel.Hide(); // Ẩn ngay lập tức để tránh lộ UI thừa trong khi chuyển scene
                    Destroy(mb.gameObject);
                }
            }
            _cachedPanels.Clear();
            
            if (Managers.GameStateManager.Instance != null)
            {
                Managers.GameStateManager.Instance.ChangeState(Managers.GameState.Login);
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene("Bootstrap");
        }
    }
}
