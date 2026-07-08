using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GameClient.EditorTools
{
    public class ShopConfigEditorWindow : EditorWindow
    {
        [System.Serializable]
        public class ShopItemData
        {
            public long id;
            public string shop_item_id;
            public string shop_type;
            public string item_code;
            public int amount;
            public string original_price; // JSON string like [{"item_code":"diamond","amount":100}]
            public bool is_discountable;
        }

        private List<ShopItemData> _shopItems = new List<ShopItemData>();
        private Vector2 _scrollPos;
        private readonly HttpClient _httpClient = new HttpClient();
        private const string AdminBaseUrl = "http://localhost:8080/api/gm/shop_items";

        [MenuItem("Tools/Shop Config Editor")]
        public static void ShowWindow()
        {
            GetWindow<ShopConfigEditorWindow>("Shop Configs");
        }

        private void OnEnable()
        {
            _ = FetchShopItemsAsync();
        }

        private async Task FetchShopItemsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync(AdminBaseUrl);
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    // Helper to wrap array into object for JsonUtility
                    string wrappedJson = "{\"items\":" + json + "}";
                    var wrapper = JsonUtility.FromJson<ShopItemsWrapper>(wrappedJson);
                    _shopItems = wrapper.items ?? new List<ShopItemData>();
                    Repaint();
                }
                else
                {
                    Debug.LogError($"[ShopConfigEditor] Failed to fetch: {response.StatusCode}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ShopConfigEditor] Error fetching: {ex.Message}");
            }
        }

        private async Task SaveShopItemAsync(ShopItemData item)
        {
            try
            {
                string json = JsonUtility.ToJson(item);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{AdminBaseUrl}/save", content);
                if (response.IsSuccessStatusCode)
                {
                    Debug.Log($"[ShopConfigEditor] Saved shop item {item.shop_item_id} successfully.");
                    _ = FetchShopItemsAsync();
                }
                else
                {
                    string errMsg = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"[ShopConfigEditor] Failed to save {item.shop_item_id}: {response.StatusCode} - {errMsg}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ShopConfigEditor] Error saving: {ex.Message}");
            }
        }

        private async Task DeleteShopItemAsync(string shopItemId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"{AdminBaseUrl}/delete?id={shopItemId}");
                if (response.IsSuccessStatusCode)
                {
                    Debug.Log($"[ShopConfigEditor] Deleted shop item {shopItemId} successfully.");
                    _ = FetchShopItemsAsync();
                }
                else
                {
                    string errMsg = await response.Content.ReadAsStringAsync();
                    Debug.LogError($"[ShopConfigEditor] Failed to delete: {response.StatusCode} - {errMsg}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ShopConfigEditor] Error deleting: {ex.Message}");
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Shop Configurations Database Editor", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh From DB", GUILayout.Width(120), GUILayout.Height(25)))
            {
                _ = FetchShopItemsAsync();
            }
            if (GUILayout.Button("Add New Shop Item", GUILayout.Width(150), GUILayout.Height(25)))
            {
                _shopItems.Add(new ShopItemData
                {
                    shop_item_id = "new_shop_item_" + System.Guid.NewGuid().ToString().Substring(0, 5),
                    shop_type = "daily",
                    item_code = "stamina_potion",
                    amount = 1,
                    original_price = "[{\"item_code\": \"diamond\", \"amount\": 50}]",
                    is_discountable = true
                });
            }
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            for (int i = 0; i < _shopItems.Count; i++)
            {
                var item = _shopItems[i];
                EditorGUILayout.BeginVertical("box");
                
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"Shop Item: {item.shop_item_id}", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                
                GUI.backgroundColor = Color.green;
                if (GUILayout.Button("Save to DB", GUILayout.Width(100)))
                {
                    _ = SaveShopItemAsync(item);
                }
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Delete", GUILayout.Width(70)))
                {
                    if (EditorUtility.DisplayDialog("Delete Confirmation", $"Are you sure you want to delete {item.shop_item_id}?", "Yes", "No"))
                    {
                        if (item.id > 0)
                        {
                            _ = DeleteShopItemAsync(item.shop_item_id);
                        }
                        else
                        {
                            _shopItems.RemoveAt(i);
                            i--;
                        }
                    }
                }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                item.shop_item_id = EditorGUILayout.TextField("Shop Item ID (Unique)", item.shop_item_id);
                item.shop_type = EditorGUILayout.TextField("Shop Type (daily/guild/arena)", item.shop_type);
                item.item_code = EditorGUILayout.TextField("Item Code", item.item_code);
                item.amount = EditorGUILayout.IntField("Amount", item.amount);
                item.original_price = EditorGUILayout.TextField("Original Price (JSON)", item.original_price);
                item.is_discountable = EditorGUILayout.Toggle("Is Discountable (Daily shop only)", item.is_discountable);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndScrollView();
        }

        [System.Serializable]
        private class ShopItemsWrapper
        {
            public List<ShopItemData> items;
        }
    }
}
