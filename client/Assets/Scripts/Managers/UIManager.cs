using System.Collections.Generic;
using System.Threading.Tasks;
using GameClient.Core;
using UnityEngine;
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

        public void ShowMessage(string title, string content, System.Action onConfirm = null)
        {
            Debug.Log($"[POPUP] {title}: {content}");
            
            if (GameClient.Managers.ToastManager.Instance != null)
            {
                GameClient.Managers.ToastManager.Instance.ShowNormalToast($"{title}: {content}");
            }
            
            if (onConfirm != null)
            {
                onConfirm.Invoke();
            }
        }

        public async Task<IUIView> OpenPanelAsync(string addressableKey, object data = null, bool isLoadByPlatform = true)
        {
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

                    uiInstance = await ResourceManager.Instance.InstantiateAsync(platformKey, canvasRoot);
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
            foreach (var panel in _cachedPanels.Values)
            {
                var mb = panel as MonoBehaviour;
                if (mb != null && mb.gameObject != null) Destroy(mb.gameObject);
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
