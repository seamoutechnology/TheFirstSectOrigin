using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections.Generic;
using System.Reflection;
using System;
using GameClient.Managers;
using TMPro;
using TFSO.Managers;

namespace GameClient.UI.Layout
{
    public class UILayoutBuilder : MonoBehaviour
    {
        [Header("Templates (Optional but recommended)")]
        [SerializeField] private GameObject panelPrefab;
        [SerializeField] private GameObject buttonPrefab;
        [SerializeField] private GameObject textPrefab;
        [SerializeField] private GameObject imagePrefab;
        [SerializeField] private GameObject videoPrefab;

        [Header("Event Target")]
        [SerializeField] private MonoBehaviour eventTarget; // Script chứa các hàm xử lý click

        private Dictionary<string, GameObject> _createdElements = new Dictionary<string, GameObject>();

        public void Build(UILayout layout, Transform root)
        {
            _createdElements.Clear();
            foreach (var element in layout.Elements)
            {
                CreateElement(element, root);
            }
        }

        public GameObject GetElement(string name)
        {
            _createdElements.TryGetValue(name, out var go);
            return go;
        }

        public void CreateElement(UIElementData data, Transform parent)
        {
            GameObject go = null;

            if (data is UIPanelData panelData)
            {
                go = panelPrefab != null ? Instantiate(panelPrefab, parent) : CreateEmptyUI("Panel", parent);
                ConfigureImage(go, panelData.ColorHex);
                
                foreach (var child in panelData.Children)
                {
                    CreateElement(child, go.transform);
                }
            }
            else if (data is UIButtonData buttonData)
            {
                go = buttonPrefab != null ? Instantiate(buttonPrefab, parent) : CreateEmptyUI("Button", parent);
                var textComp = go.GetComponentInChildren<TMP_Text>();
                if (textComp != null) textComp.text = buttonData.Text;

                var btn = go.GetComponent<Button>();
                if (btn == null) btn = go.AddComponent<Button>();
                
                if (!string.IsNullOrEmpty(buttonData.OnClickMethod) && eventTarget != null)
                {
                    Type type = eventTarget.GetType();
                    
                    MethodInfo methodWithArg = type.GetMethod(buttonData.OnClickMethod, 
                        new[] { typeof(string) });
                    
                    if (methodWithArg != null)
                    {
                        btn.onClick.AddListener(() => methodWithArg.Invoke(eventTarget, new object[] { buttonData.CommandArgument }));
                    }
                    else
                    {
                        MethodInfo methodNoArg = type.GetMethod(buttonData.OnClickMethod, Type.EmptyTypes);
                        if (methodNoArg != null)
                        {
                            btn.onClick.AddListener(() => methodNoArg.Invoke(eventTarget, null));
                        }
                        else
                        {
                            Debug.LogWarning($"[UILayoutBuilder] Không tìm thấy hàm {buttonData.OnClickMethod} (có hoặc không tham số string) trong {type.Name}");
                        }
                    }
                }
            }
            else if (data is UITextData textData)
            {
                go = textPrefab != null ? Instantiate(textPrefab, parent) : CreateEmptyUI("Text", parent);
                var textComp = go.GetComponent<TMP_Text>();
                if (textComp != null)
                {
                    string finalContent = textData.IsLocalized 
                        ? LocalizationManager.Instance.GetText(textData.Content) 
                        : textData.Content;
                        
                    textComp.text = finalContent;
                    textComp.fontSize = textData.FontSize;
                }
            }
            else if (data is UIImageData imageData)
            {
                go = imagePrefab != null ? Instantiate(imagePrefab, parent) : CreateEmptyUI("Image", parent);
            }
            else if (data is UIVideoData videoData)
            {
                go = videoPrefab != null ? Instantiate(videoPrefab, parent) : CreateEmptyUI("Video", parent);
                var vp = go.GetComponent<VideoPlayer>();
                if (vp == null) vp = go.AddComponent<VideoPlayer>();
                vp.isLooping = videoData.Loop;
                vp.renderMode = VideoRenderMode.CameraFarPlane; // Default
            }

            if (go != null)
            {
                go.name = data.Name;
                _createdElements[data.Name] = go;
                
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = data.GetAnchorMin();
                rect.anchorMax = data.GetAnchorMax();
                rect.pivot = data.GetPivot();
                rect.anchoredPosition = data.GetPosition();
                rect.sizeDelta = data.GetSize();

                ApplyStyle(go, data.StyleClass);
            }
        }

        private Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        private void ApplyStyle(GameObject go, string className)
        {
            var style = UIThemeManager.Instance.GetStyle(className);
            if (style == null) return;

            var img = go.GetComponent<Image>();
            if (img != null)
            {
                img.color = style.mainColor;
                if (!string.IsNullOrEmpty(style.spriteName))
                {
                    if (!_spriteCache.TryGetValue(style.spriteName, out Sprite sprite))
                    {
                        sprite = GameClient.Core.ResourceManager.Instance.LoadFromResources<Sprite>($"UI/Sprites/{style.spriteName}");
                        if (sprite != null) _spriteCache[style.spriteName] = sprite;
                    }
                    if (sprite != null) img.sprite = sprite;
                }
            }

            var text = go.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.color = style.textColor;
                text.fontSize = style.fontSize;
            }
        }

        private GameObject CreateEmptyUI(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private void ConfigureImage(GameObject go, string hexColor)
        {
            Image img = go.GetComponent<Image>();
            if (img == null) img = go.AddComponent<Image>();
            
            if (ColorUtility.TryParseHtmlString(hexColor, out Color color))
            {
                img.color = color;
            }
        }
    }
}
