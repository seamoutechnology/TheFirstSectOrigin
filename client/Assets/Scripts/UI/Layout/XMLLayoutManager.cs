using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using GameClient.Core;

namespace GameClient.UI.Layout
{
    public class XMLLayoutManager : Singleton<XMLLayoutManager>
    {
        private XmlSerializer _serializer;
        private System.Collections.Generic.Dictionary<string, UILayout> _layoutCache = new System.Collections.Generic.Dictionary<string, UILayout>();

        protected override void Awake()
        {
            base.Awake();
            _serializer = new XmlSerializer(typeof(UILayout));
        }

        public UILayout LoadLayout(string layoutName)
        {
            if (_layoutCache.TryGetValue(layoutName, out var cachedLayout))
            {
                return cachedLayout;
            }

            TextAsset xmlAsset = GameClient.Core.ResourceManager.Instance.LoadFromResources<TextAsset>($"Layouts/{layoutName}");
            if (xmlAsset == null)
            {
                Debug.LogError($"[XMLLayoutManager] Không tìm thấy layout: Layouts/{layoutName}");
                return null;
            }

            using (StringReader reader = new StringReader(xmlAsset.text))
            {
                UILayout layout = (UILayout)_serializer.Deserialize(reader);
                if (layout != null)
                {
                    _layoutCache[layoutName] = layout; // Lưu vào cache
                }
                return layout;
            }
        }
    }
}
