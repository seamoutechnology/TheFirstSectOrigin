using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

namespace GameClient.UI.Layout
{
    [XmlRoot("Layout")]
    public class UILayout
    {
        [XmlAttribute("name")]
        public string Name;

        [XmlElement("Panel", typeof(UIPanelData))]
        [XmlElement("Button", typeof(UIButtonData))]
        [XmlElement("Text", typeof(UITextData))]
        [XmlElement("Image", typeof(UIImageData))]
        [XmlElement("Video", typeof(UIVideoData))]
        public List<UIElementData> Elements = new List<UIElementData>();
    }

    [XmlInclude(typeof(UIPanelData))]
    [XmlInclude(typeof(UIButtonData))]
    [XmlInclude(typeof(UITextData))]
    [XmlInclude(typeof(UIImageData))]
    [XmlInclude(typeof(UIVideoData))]
    public abstract class UIElementData
    {
        [XmlAttribute("name")]
        public string Name;

        [XmlAttribute("position")]
        public string Position = "0,0";

        [XmlAttribute("size")]
        public string Size = "100,100";

        [XmlAttribute("anchorMin")]
        public string AnchorMin = "0.5,0.5";

        [XmlAttribute("anchorMax")]
        public string AnchorMax = "0.5,0.5";

        [XmlAttribute("pivot")]
        public string Pivot = "0.5,0.5";

        [XmlAttribute("style")]
        public string StyleClass;

        public Vector2 GetPosition() => ParseVector2(Position);
        public Vector2 GetSize() => ParseVector2(Size);
        public Vector2 GetAnchorMin() => ParseVector2(AnchorMin);
        public Vector2 GetAnchorMax() => ParseVector2(AnchorMax);
        public Vector2 GetPivot() => ParseVector2(Pivot);

        private Vector2 ParseVector2(string val)
        {
            if (string.IsNullOrEmpty(val)) return new Vector2(0.5f, 0.5f);
            var split = val.Split(',');
            return new Vector2(float.Parse(split[0]), float.Parse(split[1]));
        }
    }

    public class UIPanelData : UIElementData
    {
        [XmlAttribute("color")]
        public string ColorHex = "#FFFFFF";

        [XmlElement("Panel", typeof(UIPanelData))]
        [XmlElement("Button", typeof(UIButtonData))]
        [XmlElement("Text", typeof(UITextData))]
        [XmlElement("Image", typeof(UIImageData))]
        [XmlElement("Video", typeof(UIVideoData))]
        public List<UIElementData> Children = new List<UIElementData>();
    }

    public class UIButtonData : UIElementData
    {
        [XmlAttribute("text")]
        public string Text;
        
        [XmlAttribute("onClick")]
        public string OnClickMethod;

        [XmlAttribute("argument")]
        public string CommandArgument;
    }

    public class UITextData : UIElementData
    {
        [XmlAttribute("content")]
        public string Content;
        
        [XmlAttribute("fontSize")]
        public int FontSize = 14;

        [XmlAttribute("localized")]
        public bool IsLocalized = false;
    }

    public class UIImageData : UIElementData
    {
        [XmlAttribute("sprite")]
        public string SpriteName;
    }

    public class UIVideoData : UIElementData
    {
        [XmlAttribute("clipName")]
        public string ClipName;

        [XmlAttribute("loop")]
        public bool Loop = true;
    }
}
