using System;
using System.Collections.Generic;
using UnityEngine;
using GameClient.Core;

namespace GameClient.UI.Layout
{
    [Serializable]
    public class UIStyle
    {
        public string className;
        public Color mainColor = Color.white;
        public Color textColor = Color.black;
        public int fontSize = 24;
        public string spriteName;
    }

    public class UIThemeManager : Singleton<UIThemeManager>
    {
        [SerializeField] private List<UIStyle> styles = new List<UIStyle>();
        private Dictionary<string, UIStyle> _styleCache = new Dictionary<string, UIStyle>();

        protected override void Awake()
        {
            base.Awake();
            foreach (var style in styles)
            {
                _styleCache[style.className] = style;
            }
        }

        public UIStyle GetStyle(string className)
        {
            if (string.IsNullOrEmpty(className)) return null;
            _styleCache.TryGetValue(className, out var style);
            return style;
        }
    }
}
