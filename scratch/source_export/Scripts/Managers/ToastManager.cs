using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace GameClient.Managers
{
    public class ToastManager : GameClient.Singleton<ToastManager>
    {
        [Header("Toast Prefabs")]
        public GameObject normalToastPrefab;
        public GameObject bigToastPrefab;

        [Header("Containers (Optional - Auto-creates if null)")]
        public Transform normalToastContainer; 
        public Transform bigToastContainer;    

        private const string NormalToastPoolKey = "NormalToastPool";
        
        private const int MaxActiveToasts = 4;
        private readonly List<GameObject> _activeToasts = new();

        private void Start()
        {
            RegisterPoolIfNeeded();
        }

        private void RegisterPoolIfNeeded()
        {
            if (normalToastPrefab != null)
            {
                ObjectPoolManager.Instance.RegisterPool(NormalToastPoolKey, normalToastPrefab, 5, 20);
            }
        }

        private void EnsureContainers()
        {
            RegisterPoolIfNeeded();

            if (normalToastContainer == null)
            {
                var go = GameObject.Find("NormalToastContainer") ?? GameObject.Find("ToastContainer");
                if (go != null)
                {
                    normalToastContainer = go.transform;
                    
                    var layout = go.GetComponent<VerticalLayoutGroup>();
                    if (layout == null)
                    {
                        var oldLayout = go.GetComponent<LayoutGroup>();
                        if (oldLayout != null) Destroy(oldLayout);
                        
                        layout = go.AddComponent<VerticalLayoutGroup>();
                        layout.childAlignment = TextAnchor.LowerCenter;
                        layout.childControlWidth = true;
                        layout.childControlHeight = true;
                        layout.childForceExpandWidth = true;
                        layout.childForceExpandHeight = false;
                        layout.spacing = 10f;
                    }
                }
                else
                {
                    Canvas activeCanvas = FindFirstObjectByType<Canvas>();
                    if (activeCanvas != null)
                    {
                        GameObject containerGo = new GameObject("NormalToastContainer");
                        containerGo.transform.SetParent(activeCanvas.transform, false);
                        
                        var layout = containerGo.AddComponent<VerticalLayoutGroup>();
                        layout.childAlignment = TextAnchor.LowerCenter;
                        layout.childControlWidth = true;
                        layout.childControlHeight = true;
                        layout.childForceExpandWidth = true;
                        layout.childForceExpandHeight = false;
                        layout.spacing = 10f;
                        
                        var fitter = containerGo.AddComponent<ContentSizeFitter>();
                        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                        
                        RectTransform rt = containerGo.GetComponent<RectTransform>();
                        rt.anchorMin = new Vector2(0.2f, 0.05f);
                        rt.anchorMax = new Vector2(0.8f, 0.4f);
                        rt.pivot = new Vector2(0.5f, 0f);
                        rt.anchoredPosition = Vector2.zero;
                        
                        normalToastContainer = containerGo.transform;
                    }
                }
            }

            if (bigToastContainer == null)
            {
                var go = GameObject.Find("BigToastContainer");
                if (go != null)
                {
                    bigToastContainer = go.transform;
                }
                else
                {
                    bigToastContainer = normalToastContainer;
                }
            }
        }

        public void ShowNormalToast(string message, float duration = 2.0f)
        {
            EnsureContainers();

            if (normalToastPrefab == null || normalToastContainer == null)
            {
                Debug.LogWarning($"[Toast] {message}");
                return;
            }

            _activeToasts.RemoveAll(item => item == null || !item.activeSelf);

            while (_activeToasts.Count >= MaxActiveToasts)
            {
                var oldest = _activeToasts[0];
                _activeToasts.RemoveAt(0);

                if (oldest != null && oldest.activeSelf)
                {
                    var toastItem = oldest.GetComponent<GameClient.UI.Core.ToastItem>();
                    if (toastItem != null)
                    {
                        toastItem.DismissImmediate();
                    }
                }
            }

            GameObject toastObj = ObjectPoolManager.Instance.Get(NormalToastPoolKey, normalToastContainer);
            if (toastObj != null)
            {
                RectTransform rt = toastObj.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchoredPosition = Vector2.zero;
                    rt.sizeDelta = Vector2.zero; // Set Left/Right/Top/Bottom offset về 0
                    rt.localScale = Vector3.one;
                }

                toastObj.transform.SetAsLastSibling(); // Xếp xuống dưới cùng của Layout
                var toastItem = toastObj.GetComponent<GameClient.UI.Core.ToastItem>();
                if (toastItem != null)
                {
                    toastItem.Setup(message, duration, NormalToastPoolKey, false);
                }
                
                _activeToasts.Add(toastObj);
            }
        }

        public void ShowBigToast(string message, float duration = 4.0f)
        {
            EnsureContainers();

            if (bigToastPrefab == null || bigToastContainer == null)
            {
                Debug.LogWarning($"[BIG TOAST] {message}");
                return;
            }

            GameObject toastObj = Instantiate(bigToastPrefab, bigToastContainer);
            var toastItem = toastObj.GetComponent<GameClient.UI.Core.ToastItem>();
            if (toastItem != null)
            {
                toastItem.Setup(message, duration, null, true);
            }
        }
    }
}
