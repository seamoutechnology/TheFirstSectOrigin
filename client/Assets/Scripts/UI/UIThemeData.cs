using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GameClient.UI
{
    public enum UIStyleType
    {
        Title,
        Header,
        Body,
        SubText,
        ButtonText,
        TextInput
    }

    [Serializable]
    public struct UIStyleConfig
    {
        public UIStyleType styleType;
        public TMP_FontAsset fontAsset;
        public float fontSize;
        public Color textColor;
        
        [Header("Outline settings")]
        public bool useOutline;
        public float outlineWidth;
        public Color outlineColor;

        [Header("Face settings")]
        public float faceDilate;
    }

    [CreateAssetMenu(fileName = "NewUIThemeData", menuName = "UI/Theme Data")]
    public class UIThemeData : ScriptableObject
    {
        [Header("Danh sách cấu hình kiểu chữ")]
        public List<UIStyleConfig> styles = new List<UIStyleConfig>();

        public bool TryGetStyle(UIStyleType type, out UIStyleConfig config)
        {
            for (int i = 0; i < styles.Count; i++)
            {
                if (styles[i].styleType == type)
                {
                    config = styles[i];
                    return true;
                }
            }
            config = default;
            return false;
        }
    }
}
