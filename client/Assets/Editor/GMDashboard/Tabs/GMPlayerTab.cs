using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Collections.Generic;

namespace GameClient.Editor.GMDashboard
{
    public class GMPlayerTab
    {
        private EditorWindow window;
        private string AdminUrl => GMDashboardConfig.GmApiUrl;
        
        // & Pagination State
        private List<GMUserListItem> userList = new List<GMUserListItem>();
        private int currentPage = 1;
        private int limit = 20;
        private int totalUsers = 0;
        private string searchKeyword = "";
        private int[] limitOptions = { 10, 20, 50, 100 };
        private int limitIndex = 1; // Default to 20
        private Vector2 leftScrollPos;
        
        // state
        private List<GMZoneDB> zoneList = new List<GMZoneDB>();
        private string[] zoneOptions = new string[] { "Đang tải Zone..." };
        private int selectedZoneIndex = 0;
        private int CurrentZoneId => zoneList.Count > 0 ? zoneList[selectedZoneIndex].id : 1;
        
        // state
        private float leftPanelWidth = 400f;
        private bool isResizing = false;
        
        // state
        private GMUserInfo currentUser;
        private List<GMUserItem> currentInventory = new List<GMUserItem>();
        
        // item Form
        private string newItemCode = "gold";
        private int newItemQuantity = 1;
        private List<GMItemConfigData> availableItemConfigs = new List<GMItemConfigData>();
        private string[] itemOptions = new string[] { "gold (Vàng)", "qi (Linh Khí)", "diamond (Kim Cương)" };
        private int selectedItemConfigIndex = 0;

        private Vector2 rightScrollPos;

        public GMPlayerTab(EditorWindow window)
        {
            this.window = window;
        }

        public void OnEnable()
        {
            FetchUserList();
            FetchZoneList();
            FetchAvailableItems();
            // Auto re-fetch when server comes online
            GMDashboardConfig.OnStatusChanged += OnServerStatusChanged;
        }

        public void OnDisable()
        {
            GMDashboardConfig.OnStatusChanged -= OnServerStatusChanged;
        }

        private void OnServerStatusChanged()
        {
            if (GMDashboardConfig.Status == GMDashboardConfig.ConnectionStatus.Online)
            {
                // Refetch zones if the list is still empty
                if (zoneList.Count == 0) FetchZoneList();
                if (availableItemConfigs.Count == 0) FetchAvailableItems();
            }
        }

        private void FetchAvailableItems()
        {
            string url = $"{AdminUrl}/item_configs";
            var req = UnityWebRequest.Get(url);
            var op = req.SendWebRequest();
            
            op.completed += (asyncOp) =>
            {
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var items = GMJsonHelper.FromJson<GMItemConfigData>(req.downloadHandler.text);
                    availableItemConfigs = new List<GMItemConfigData>(items ?? new GMItemConfigData[0]);
                    if (availableItemConfigs.Count > 0)
                    {
                        itemOptions = new string[availableItemConfigs.Count];
                        for (int i = 0; i < availableItemConfigs.Count; i++)
                        {
                            itemOptions[i] = $"{availableItemConfigs[i].item_code} ({availableItemConfigs[i].name_key})";
                        }
                        newItemCode = availableItemConfigs[0].item_code;
                    }
                    window.Repaint();
                }
                req.Dispose();
            };
        }

        private void FetchZoneList()
        {
            string url = $"{AdminUrl}/zones";
            var req = UnityWebRequest.Get(url);
            var op = req.SendWebRequest();
            
            op.completed += (asyncOp) =>
            {
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var zones = GMJsonHelper.FromJson<GMZoneDB>(req.downloadHandler.text);
                    zoneList = new List<GMZoneDB>(zones);
                    zoneOptions = new string[zoneList.Count];
                    for (int i = 0; i < zoneList.Count; i++)
                    {
                        zoneOptions[i] = zoneList[i].name;
                    }
                    window.Repaint();
                }
                else
                {
                    Debug.LogError("[GM API] Fetch Zones failed: " + req.error);
                }
                req.Dispose();
            };
        }

        public void OnGUI()
        {
            GUILayout.BeginHorizontal();

            DrawLeftPanel();
            DrawSplitter();
            DrawRightPanel();

            GUILayout.EndHorizontal();
        }

        private void DrawSplitter()
        {
            GUILayout.Box("", GUILayout.Width(5), GUILayout.ExpandHeight(true));
            Rect splitterRect = GUILayoutUtility.GetLastRect();
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

            Event e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (splitterRect.Contains(e.mousePosition))
                    {
                        isResizing = true;
                        e.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (isResizing)
                    {
                        leftPanelWidth += e.delta.x;
                        leftPanelWidth = Mathf.Clamp(leftPanelWidth, 300, window.position.width - 200);
                        window.Repaint();
                        e.Use();
                    }
                    break;
                case EventType.MouseUp:
                    if (isResizing)
                    {
                        isResizing = false;
                        e.Use();
                    }
                    break;
            }
        }

        private void DrawLeftPanel()
        {
            GUILayout.BeginVertical("box", GUILayout.Width(leftPanelWidth));
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("Danh sách Người dùng", EditorStyles.boldLabel);
            
            // Zone selector
            GUILayout.FlexibleSpace();
            GUILayout.Label("Zone:", GUILayout.Width(40));

            if (zoneList.Count == 0)
            {
                // Server belum return zones — show fallback
                GUI.color = new Color(1f, 0.8f, 0.4f);
                GUILayout.Label("(chưa tải)", GUILayout.Width(80));
                GUI.color = Color.white;
                if (GUILayout.Button("↺", GUILayout.Width(26))) FetchZoneList();
            }
            else
            {
                int newZoneIndex = EditorGUILayout.Popup(selectedZoneIndex, zoneOptions, GUILayout.Width(130));
                if (newZoneIndex != selectedZoneIndex)
                {
                    selectedZoneIndex = newZoneIndex;
                    if (currentUser != null)
                    {
                        FetchUserData(currentUser.user_id);
                        FetchUserInventory();
                    }
                }
                if (GUILayout.Button("↺", GUILayout.Width(26))) FetchZoneList();
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            searchKeyword = GUILayout.TextField(searchKeyword, GUILayout.Width(150));
            if (GUILayout.Button("Tìm", GUILayout.Width(50)))
            {
                currentPage = 1;
                FetchUserList();
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            GUILayout.Label("ID", GUILayout.Width(40));
            GUILayout.Label("Nickname", GUILayout.Width(100));
            GUILayout.Label("Level", GUILayout.Width(40));
            GUILayout.Label("Email", GUILayout.Width(140));
            GUILayout.EndHorizontal();

            leftScrollPos = GUILayout.BeginScrollView(leftScrollPos, GUILayout.Height(window.position.height - 180));
            foreach (var user in userList)
            {
                GUILayout.BeginHorizontal("box");
                GUILayout.Label(user.user_id.ToString(), GUILayout.Width(40));
                GUILayout.Label(user.nickname, GUILayout.Width(100));
                GUILayout.Label(user.level.ToString(), GUILayout.Width(40));
                GUILayout.Label(user.email, GUILayout.Width(120));
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    SelectUser(user.user_id);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            EditorGUILayout.Space();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("<", GUILayout.Width(30)) && currentPage > 1)
            {
                currentPage--;
                FetchUserList();
            }
            int maxPage = Mathf.CeilToInt((float)totalUsers / limit);
            if (maxPage == 0) maxPage = 1;
            GUILayout.Label($"Trang {currentPage} / {maxPage}", GUILayout.Width(80));
            if (GUILayout.Button(">", GUILayout.Width(30)) && currentPage < maxPage)
            {
                currentPage++;
                FetchUserList();
            }

            GUILayout.FlexibleSpace();
            GUILayout.Label("Hiển thị:", GUILayout.Width(60));
            string[] limitStrings = new string[limitOptions.Length];
            for (int i = 0; i < limitOptions.Length; i++) limitStrings[i] = limitOptions[i].ToString();
            
            int newIndex = EditorGUILayout.Popup(limitIndex, limitStrings, GUILayout.Width(50));
            if (newIndex != limitIndex)
            {
                limitIndex = newIndex;
                limit = limitOptions[limitIndex];
                currentPage = 1;
                FetchUserList();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawRightPanel()
        {
            GUILayout.BeginVertical("box");
            rightScrollPos = GUILayout.BeginScrollView(rightScrollPos);

            if (currentUser == null)
            {
                GUILayout.Label("Vui lòng chọn 1 User từ danh sách bên trái.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
                return;
            }

            GUILayout.BeginVertical("box");
            GUILayout.Label($"Thông tin User: {currentUser.user_id}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Email:", currentUser.email);
            EditorGUILayout.LabelField("Sect Name:", currentUser.sect_name);
            EditorGUILayout.LabelField("Level:", currentUser.level.ToString());
            EditorGUILayout.LabelField("Bind Coin:", currentUser.money.ToString());
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            GUILayout.BeginVertical("box");
            GUILayout.Label("Thêm Vật phẩm", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Mã đồ:", GUILayout.Width(80));

            if (itemOptions != null && itemOptions.Length > 0)
            {
                selectedItemConfigIndex = EditorGUILayout.Popup(selectedItemConfigIndex, itemOptions, GUILayout.Width(180));
                if (selectedItemConfigIndex >= 0 && selectedItemConfigIndex < availableItemConfigs.Count)
                {
                    newItemCode = availableItemConfigs[selectedItemConfigIndex].item_code;
                }
            }
            else
            {
                newItemCode = GUILayout.TextField(newItemCode, GUILayout.Width(180));
            }

            GUILayout.Label("SL:", GUILayout.Width(30));
            newItemQuantity = EditorGUILayout.IntField(newItemQuantity, GUILayout.Width(50));
            bool playerOnline = GMDashboardConfig.Status == GMDashboardConfig.ConnectionStatus.Online;
            GUI.enabled = playerOnline && currentUser != null;
            if (GUILayout.Button("Thêm", GUILayout.Width(80)))
            {
                AddItemToUser();
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            EditorGUILayout.Space();

            GUILayout.BeginVertical("box");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Túi đồ (Inventory)", EditorStyles.boldLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(80)))
            {
                FetchUserInventory();
            }
            GUILayout.EndHorizontal();
            
            if (currentInventory.Count == 0)
            {
                GUILayout.Label("Túi đồ trống.");
            }
            else
            {
                foreach (var item in currentInventory)
                {
                    GUILayout.BeginHorizontal("box");
                    GUILayout.Label(item.item_code, GUILayout.Width(150));
                    GUILayout.Label("x" + item.quantity, GUILayout.Width(50));
                    bool canDelete = GMDashboardConfig.Status == GMDashboardConfig.ConnectionStatus.Online;
                    GUI.enabled = canDelete;
                    if (GUILayout.Button("Delete", GUILayout.Width(60)))
                    {
                        RemoveItemFromUser(item.id);
                    }
                    GUI.enabled = true;
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndVertical();
            
            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void FetchUserList()
        {
            string url = $"{AdminUrl}/users/list?page={currentPage}&limit={limit}&search={searchKeyword}";
            var req = UnityWebRequest.Get(url);
            var op = req.SendWebRequest();
            
            op.completed += (asyncOp) =>
            {
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var resp = JsonUtility.FromJson<GMUserListResponse>(req.downloadHandler.text);
                    userList = new List<GMUserListItem>(resp.data ?? new GMUserListItem[0]);
                    totalUsers = resp.total;
                    currentPage = resp.page;
                    window.Repaint();
                }
            };
        }

        private void SelectUser(long userId)
        {
            currentUser = new GMUserInfo { user_id = userId }; 
            FetchUserData(userId);
            FetchUserInventory(userId);
        }

        private void FetchUserData(long userId)
        {
            string url = $"{AdminUrl}/user?id={userId}&zone_id={CurrentZoneId}";
            var req = UnityWebRequest.Get(url);
            var op = req.SendWebRequest();
            
            op.completed += (asyncOp) =>
            {
                if (req.result == UnityWebRequest.Result.Success)
                {
                    currentUser = JsonUtility.FromJson<GMUserInfo>(req.downloadHandler.text);
                    window.Repaint();
                }
            };
        }

        private void FetchUserInventory(long userId)
        {
            string url = $"{AdminUrl}/inventory?id={userId}&zone_id={CurrentZoneId}";
            var req = UnityWebRequest.Get(url);
            var op = req.SendWebRequest();
            
            op.completed += (asyncOp) =>
            {
                if (req.result == UnityWebRequest.Result.Success)
                {
                    var array = GMJsonHelper.FromJson<GMUserItem>(req.downloadHandler.text);
                    currentInventory = new List<GMUserItem>(array ?? new GMUserItem[0]);
                    window.Repaint();
                }
            };
        }

        private void FetchUserInventory()
        {
            if (currentUser != null) {
                FetchUserInventory(currentUser.user_id);
            }
        }

        private void AddItemToUser()
        {
            if (currentUser == null) return;
            string url = $"{AdminUrl}/inventory/add?id={currentUser.user_id}&zone_id={CurrentZoneId}";
            string jsonBody = $"{{\"item_code\":\"{newItemCode}\", \"quantity\":{newItemQuantity}}}";
            
            var req = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            var op = req.SendWebRequest();
            op.completed += (asyncOp) =>
            {
                if (req.result == UnityWebRequest.Result.Success)
                {
                    FetchUserInventory();
                }
                else
                {
                    Debug.LogError($"[GM API] AddItemToUser failed: {req.error}\n{req.downloadHandler.text}");
                }
            };
        }

        private void RemoveItemFromUser(long itemId)
        {
            string url = $"{AdminUrl}/inventory/remove?item_id={itemId}&zone_id={CurrentZoneId}";
            var req = new UnityWebRequest(url, "POST");
            req.downloadHandler = new DownloadHandlerBuffer();
            
            var op = req.SendWebRequest();
            op.completed += (asyncOp) =>
            {
                if (req.result == UnityWebRequest.Result.Success)
                {
                    FetchUserInventory();
                }
                else
                {
                    Debug.LogError($"[GM API] RemoveItemFromUser failed: {req.error}\n{req.downloadHandler.text}");
                }
            };
        }
    }
}
