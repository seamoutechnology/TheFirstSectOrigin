using UnityEngine;
using System.Collections.Generic;
using TFSO.Core;
using GameClient.Network.Pb; // Uses the generated Protobuf classes

namespace GameClient.Managers
{
    public class ItemDataManager : Singleton<ItemDataManager>
    {
        private Dictionary<string, ItemConfig> _itemConfigs = new Dictionary<string, ItemConfig>();

        protected override void Awake()
        {
            base.Awake();
        }

        public void LoadConfigs(IEnumerable<ItemConfig> configs)
        {
            foreach (var config in configs)
            {
                _itemConfigs[config.ItemCode] = config;
            }
            Debug.Log($"[ItemData] Đã nạp thành công {_itemConfigs.Count} cấu hình vật phẩm.");
        }

        public void AddOrUpdateConfig(ItemConfig config)
        {
            if (config != null && !string.IsNullOrEmpty(config.ItemCode))
            {
                _itemConfigs[config.ItemCode] = config;
            }
        }

        public ItemConfig GetItemConfig(string itemCode)
        {
            if (_itemConfigs.TryGetValue(itemCode, out ItemConfig config))
            {
                return config;
            }
            Debug.LogWarning($"[ItemData] Không tìm thấy Config cho item_code: {itemCode}");
            return null;
        }

        public void RegisterTemporarySource(string itemCode, ItemSource source)
        {
            if (_itemConfigs.TryGetValue(itemCode, out ItemConfig config))
            {
                config.Sources.Add(source);
                Debug.Log($"[ItemData] Đã đăng ký thêm nguồn: '{source.LabelKey}' cho vật phẩm {itemCode}");
            }
        }
    }
}
