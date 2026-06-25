using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Collections.Generic;
using System;

namespace GameClient.Editor.GMDashboard
{
    public class GMNoticeTab
    {
        private EditorWindow window;
        private string AdminUrl => GMDashboardConfig.GmApiUrl;
        
        private List<GMAnnouncementData> noticeList = new List<GMAnnouncementData>();
        private GMAnnouncementData selectedNotice = null;
        private int selectedIndex = -1;

        // scroll
        private Vector2 leftScrollPos;
        private Vector2 rightScrollPos;

        private string[] typeOptions = new string[] { "MAINTENANCE", "RULES", "ACTIVITY", "NEWS" };

        public GMNoticeTab(EditorWindow window)
        {
            this.window = window;
        }

        public void OnEnable()
        {
            FetchAllNotices();
        }

        public void OnGUI()
        {
            GUILayout.BeginHorizontal();

            // --- LEFT PANEL (List) ---
            GUILayout.BeginVertical(GUILayout.Width(250));
            GUILayout.Label("Danh sách Thông báo", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Tải lại / Làm mới"))
            {
                FetchAllNotices();
            }

            if (GUILayout.Button("Tạo Thông báo Mới"))
            {
                CreateNewNotice();
            }

            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Load/Paste JSON"))
            {
                PasteFromJson();
            }
            if (GUILayout.Button("Export JSON"))
            {
                ExportToJson();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            leftScrollPos = GUILayout.BeginScrollView(leftScrollPos, "box");
            for (int i = 0; i < noticeList.Count; i++)
            {
                var notice = noticeList[i];
                string displayName = $"[{notice.type}] {notice.title}";
                if (!notice.is_active) displayName += " (Inactive)";
                
                GUIStyle style = (selectedIndex == i) ? EditorStyles.whiteBoldLabel : EditorStyles.label;
                
                if (GUILayout.Button(displayName, style))
                {
                    selectedIndex = i;
                    selectedNotice = notice;
                    GUI.FocusControl(null);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            // --- RIGHT PANEL (Details) ---
            GUILayout.BeginVertical();
            rightScrollPos = GUILayout.BeginScrollView(rightScrollPos, "box");

            if (selectedNotice != null)
            {
                GUILayout.Label("Chi tiết Thông báo", EditorStyles.boldLabel);
                GUILayout.Space(10);

                selectedNotice.title = EditorGUILayout.TextField("Tiêu đề", selectedNotice.title);
                
                int typeIdx = Array.IndexOf(typeOptions, selectedNotice.type);
                if (typeIdx < 0) typeIdx = 0;
                typeIdx = EditorGUILayout.Popup("Loại", typeIdx, typeOptions);
                selectedNotice.type = typeOptions[typeIdx];

                EditorGUILayout.LabelField("Nội dung:");
                selectedNotice.content = EditorGUILayout.TextArea(selectedNotice.content, GUILayout.Height(150));

                selectedNotice.start_at = EditorGUILayout.TextField("Bắt đầu (ISO8601)", selectedNotice.start_at);
                selectedNotice.end_at = EditorGUILayout.TextField("Kết thúc (ISO8601)", selectedNotice.end_at);
                selectedNotice.is_active = EditorGUILayout.Toggle("Kích hoạt", selectedNotice.is_active);

                GUILayout.Space(20);
                GUILayout.BeginHorizontal();
                bool noticeOnline = GMDashboardConfig.Status == GMDashboardConfig.ConnectionStatus.Online;
                GUI.enabled = noticeOnline;
                if (GUILayout.Button("LƯU THÔNG BÁO", GUILayout.Height(30), GUILayout.Width(150)))
                {
                    SaveNotice(selectedNotice);
                }
                if (GUILayout.Button("XÓA", GUILayout.Height(30), GUILayout.Width(80)))
                {
                    if (EditorUtility.DisplayDialog("Xóa Thông Báo", "Bạn có chắc muốn xóa thông báo này không?", "Đồng ý", "Hủy"))
                    {
                        DeleteNotice(selectedNotice);
                    }
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("Chọn một thông báo bên trái hoặc Tạo mới.");
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void CreateNewNotice()
        {
            selectedNotice = new GMAnnouncementData()
            {
                id = 0,
                type = "NEWS",
                title = "Thông báo mới",
                content = "Nội dung...",
                start_at = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                end_at = DateTime.UtcNow.AddDays(7).ToString("yyyy-MM-ddTHH:mm:ssZ"),
                is_active = true
            };
            selectedIndex = -1;
        }

        private void FetchAllNotices()
        {
            string url = AdminUrl + "/notices";
            var request = UnityWebRequest.Get(url);
            
            request.SendWebRequest().completed += (asyncOp) =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    var array = GMJsonHelper.FromJson<GMAnnouncementData>(request.downloadHandler.text);
                    noticeList = new List<GMAnnouncementData>(array ?? new GMAnnouncementData[0]);
                    window.Repaint();
                }
                else
                {
                    Debug.LogError($"[GM Dashboard] Fetch failed: {request.error}");
                    EditorUtility.DisplayDialog("Lỗi Mạng", $"Không thể tải thông báo từ Admin Server ({url}).\n\nLỗi: {request.error}\n\nServer GM của bạn đã chạy chưa?", "OK");
                }
            };
        }

        private void SaveNotice(GMAnnouncementData notice)
        {
            string url = AdminUrl + "/notices/save";
            string json = JsonUtility.ToJson(notice);
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            request.SendWebRequest().completed += (asyncOp) =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    EditorUtility.DisplayDialog("Lưu thành công", $"Thông báo '{notice.title}' đã được bắn lên Server!", "OK");
                    FetchAllNotices();
                }
                else
                {
                    EditorUtility.DisplayDialog("Lỗi", $"Lưu thất bại: {request.error}", "OK");
                }
            };
        }

        private void DeleteNotice(GMAnnouncementData notice)
        {
            if (notice.id <= 0)
            {
                selectedNotice = null;
                return;
            }

            string url = AdminUrl + "/notices/delete";
            string json = "{\"id\":" + notice.id + "}";
            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            request.SendWebRequest().completed += (asyncOp) =>
            {
                if (request.result == UnityWebRequest.Result.Success)
                {
                    noticeList.Remove(notice);
                    selectedNotice = null;
                    window.Repaint();
                    EditorUtility.DisplayDialog("Xóa thành công", $"Đã xóa thông báo trên Server!", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Lỗi", $"Xóa thất bại: {request.error}", "OK");
                }
            };
        }

        private void PasteFromJson()
        {
            string json = EditorGUIUtility.systemCopyBuffer;
            if (string.IsNullOrWhiteSpace(json))
            {
                EditorUtility.DisplayDialog("Lỗi", "Clipboard trống! Hãy copy chuỗi JSON trước.", "OK");
                return;
            }

            try
            {
                var array = GMJsonHelper.FromJson<GMAnnouncementData>(json);
                if (array != null)
                {
                    noticeList = new List<GMAnnouncementData>(array);
                    window.Repaint();
                    EditorUtility.DisplayDialog("Thành công", $"Đã load {noticeList.Count} thông báo từ Clipboard. Bấm 'Lưu Thông Báo' cho từng cái để lưu lên Server nếu muốn.", "OK");
                }
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Lỗi Format", "Chuỗi JSON không đúng định dạng!\n" + ex.Message, "OK");
            }
        }

        private void ExportToJson()
        {
            if (noticeList == null || noticeList.Count == 0)
            {
                EditorUtility.DisplayDialog("Lỗi", "Danh sách thông báo trống!", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel("Lưu file Notice JSON", "", "notices.json", "json");
            if (!string.IsNullOrEmpty(path))
            {
                try
                {
                    // tạo dummy wrapper object có chứa field 'data' để serialize giống hệt mảng
                    string json = "{ \"data\": [";
                    for(int i=0; i<noticeList.Count; i++)
                    {
                        json += JsonUtility.ToJson(noticeList[i]);
                        if(i < noticeList.Count - 1) json += ",";
                    }
                    json += "] }";
                    
                    // lấy ra phần mảng bên trong
                    int startIndex = json.IndexOf('[');
                    int endIndex = json.LastIndexOf(']');
                    string finalJson = json.Substring(startIndex, endIndex - startIndex + 1);

                    System.IO.File.WriteAllText(path, finalJson);
                    EditorGUIUtility.systemCopyBuffer = finalJson;
                    
                    EditorUtility.DisplayDialog("Thành công", "Đã lưu JSON tại:\n" + path + "\n(Và đã copy vào Clipboard)", "OK");
                }
                catch (Exception ex)
                {
                    EditorUtility.DisplayDialog("Lỗi", "Không thể lưu file: " + ex.Message, "OK");
                }
            }
        }
    }
}
