using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace GameClient.EditorTools
{
    public class GiftCodeGeneratorWindow : EditorWindow
    {
        private const string API_GET_ITEMS = "http://localhost:8080/api/gm/item_configs";
        private const string API_CREATE_GIFTCODE = "http://localhost:8080/api/gm/gift_code/create";

        private string _code = "SECT888";
        private int _rewardGold = 10000;
        private int _rewardXu = 100; // corresponds to reward_diamond
        private int _maxUses = 100;

        [System.Serializable]
        public class ItemReward
        {
            public string item_code;
            public int quantity;
        }

        private List<ItemReward> _rewardsList = new List<ItemReward>();
        private List<string> _availableItemCodes = new List<string>();
        private List<string> _availableItemNames = new List<string>();
        private bool _isItemConfigsLoaded = false;
        private Vector2 _scrollPos;

        [MenuItem("Tools/Gift Code Generator")]
        public static void ShowWindow()
        {
            var window = GetWindow<GiftCodeGeneratorWindow>();
            window.titleContent = new GUIContent("GiftCode GM Tool");
            window.minSize = new Vector2(500, 400);
            window.Show();
        }

        private void OnEnable()
        {
            LoadItemConfigs();
        }

        [System.Serializable]
        public class ServerItemConfig
        {
            public string item_code;
            public string name_key;
        }

        [System.Serializable]
        public class ServerItemConfigsResponse
        {
            public List<ServerItemConfig> items;
        }

        private void LoadItemConfigs()
        {
            _availableItemCodes.Clear();
            _availableItemNames.Clear();
            _isItemConfigsLoaded = false;

            try
            {
                using (var client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    string rawJson = client.DownloadString(API_GET_ITEMS);
                    string wrappedJson = "{ \"items\": " + rawJson + " }";

                    var response = JsonUtility.FromJson<ServerItemConfigsResponse>(wrappedJson);
                    if (response != null && response.items != null)
                    {
                        foreach (var item in response.items)
                        {
                            _availableItemCodes.Add(item.item_code);
                            _availableItemNames.Add($"{item.item_code} ({item.name_key})");
                        }
                        _isItemConfigsLoaded = _availableItemCodes.Count > 0;
                        Debug.Log($"[GiftCodeGenerator] Nạp thành công {_availableItemCodes.Count} vật phẩm từ Admin Server.");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GiftCodeGenerator] Không thể kết nối tới Admin Server để lấy danh sách vật phẩm: {ex.Message}");
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Tạo mã GiftCode GM", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            // Cấu hình chung
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Cấu hình mã Code", EditorStyles.boldLabel);
            _code = EditorGUILayout.TextField("Mã GiftCode", _code).Trim().ToUpper();
            _rewardGold = EditorGUILayout.IntField("Thưởng Vàng", _rewardGold);
            _rewardXu = EditorGUILayout.IntField("Thưởng Xu", _rewardXu);
            _maxUses = EditorGUILayout.IntField("Lượt sử dụng tối đa", _maxUses);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();

            // Cấu hình danh sách vật phẩm thưởng
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Vật Phẩm Đính Kèm", EditorStyles.boldLabel);
            if (GUILayout.Button("Tải lại Item List", GUILayout.Width(120)))
            {
                LoadItemConfigs();
            }
            GUILayout.EndHorizontal();

            if (_rewardsList.Count == 0)
            {
                EditorGUILayout.HelpBox("Không có vật phẩm nào được đính kèm. Mã code này chỉ thưởng Vàng và Xu.", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < _rewardsList.Count; i++)
                {
                    var reward = _rewardsList[i];
                    EditorGUILayout.BeginHorizontal();

                    if (_isItemConfigsLoaded)
                    {
                        int currentIndex = _availableItemCodes.IndexOf(reward.item_code);
                        if (currentIndex < 0) currentIndex = 0;

                        int newIndex = EditorGUILayout.Popup("Vật phẩm", currentIndex, _availableItemNames.ToArray());
                        reward.item_code = _availableItemCodes[newIndex];
                    }
                    else
                    {
                        reward.item_code = EditorGUILayout.TextField("Mã Item", reward.item_code);
                    }

                    reward.quantity = EditorGUILayout.IntField("Số lượng", reward.quantity, GUILayout.Width(150));

                    GUI.backgroundColor = Color.red;
                    if (GUILayout.Button("-", GUILayout.Width(25)))
                    {
                        _rewardsList.RemoveAt(i);
                        break;
                    }
                    GUI.backgroundColor = Color.white;

                    EditorGUILayout.EndHorizontal();
                }
            }

            if (GUILayout.Button("+ Thêm Vật Phẩm"))
            {
                _rewardsList.Add(new ItemReward
                {
                    item_code = _availableItemCodes.Count > 0 ? _availableItemCodes[0] : "",
                    quantity = 1
                });
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("PHÁT HÀNH GIFTCODE LÊN SERVER", GUILayout.Height(40)))
            {
                SubmitGiftCodeToServer();
            }
            GUI.backgroundColor = Color.white;
        }

        [System.Serializable]
        private class ServerGiftCodeCreateRequest
        {
            public string code;
            public long reward_gold;
            public long reward_diamond;
            public string reward_items;
            public int max_uses;
        }

        private void SubmitGiftCodeToServer()
        {
            if (string.IsNullOrEmpty(_code))
            {
                EditorUtility.DisplayDialog("Lỗi", "Mã GiftCode không được để trống!", "OK");
                return;
            }

            // Build items JSON
            string itemsJson = "[]";
            if (_rewardsList.Count > 0)
            {
                // Simple manually formatted JSON to avoid utility complications
                var sb = new StringBuilder();
                sb.Append("[");
                for (int i = 0; i < _rewardsList.Count; i++)
                {
                    sb.Append($"{{\"item_code\":\"{_rewardsList[i].item_code}\",\"quantity\":{_rewardsList[i].quantity}}}");
                    if (i < _rewardsList.Count - 1)
                    {
                        sb.Append(",");
                    }
                }
                sb.Append("]");
                itemsJson = sb.ToString();
            }

            var requestData = new ServerGiftCodeCreateRequest
            {
                code = _code,
                reward_gold = _rewardGold,
                reward_diamond = _rewardXu,
                reward_items = itemsJson,
                max_uses = _maxUses
            };

            string payload = JsonUtility.ToJson(requestData);

            try
            {
                using (var client = new WebClient())
                {
                    client.Encoding = Encoding.UTF8;
                    client.Headers[HttpRequestHeader.ContentType] = "application/json";
                    string responseStr = client.UploadString(API_CREATE_GIFTCODE, "POST", payload);

                    Debug.Log($"[GiftCodeGenerator] Phản hồi từ Server: {responseStr}");
                    EditorUtility.DisplayDialog("Thành công", $"Đã tạo mã GiftCode {_code} thành công trên Server!", "OK");
                }
            }
            catch (WebException webEx)
            {
                string errorMsg = webEx.Message;
                if (webEx.Response != null)
                {
                    using (var reader = new StreamReader(webEx.Response.GetResponseStream()))
                    {
                        errorMsg = reader.ReadToEnd();
                    }
                }
                Debug.LogError($"[GiftCodeGenerator] Lỗi WebClient: {errorMsg}");
                EditorUtility.DisplayDialog("Thất bại", $"Lỗi khi tạo mã code: {errorMsg}", "OK");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GiftCodeGenerator] Lỗi: {ex.Message}");
                EditorUtility.DisplayDialog("Thất bại", $"Lỗi: {ex.Message}", "OK");
            }
        }
    }
}
