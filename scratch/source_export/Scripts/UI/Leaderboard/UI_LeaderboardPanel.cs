using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using GameClient.UI.Core;
using GameClient.Network.Api;

namespace GameClient.UI
{
    public class UI_LeaderboardPanel : BaseUIPanel
    {
        [Header("Cấu hình Prefab")]
        [SerializeField] private UI_LeaderboardItem itemPrefab;
        [SerializeField] private Transform contentContainer;
        [SerializeField] private Button btnClose;

        private List<UI_LeaderboardItem> activeItems = new List<UI_LeaderboardItem>();

        protected override void OnStart()
        {
            base.OnStart();

            if (btnClose != null)
            {
                btnClose.onClick.AddListener(ClosePanel);
            }

            if (itemPrefab != null)
            {
                itemPrefab.gameObject.SetActive(false); // Ẩn mẫu gốc
            }
        }

        protected override void OnShow()
        {
            base.OnShow();
            LoadLeaderboardData();
        }

        private async void LoadLeaderboardData()
        {
            ClearLeaderboard();

            try
            {
                // Gọi API lấy bảng xếp hạng chiến lực ("power")
                var response = await SectBuildingApi.GetLeaderboardAsync("power");

                if (response != null && response.Base != null && response.Base.Code == 0)
                {
                    int rankCounter = 1;
                    foreach (var entry in response.Entries)
                    {
                        if (entry == null) continue;

                        UI_LeaderboardItem newItem = Instantiate(itemPrefab, contentContainer);
                        newItem.gameObject.SetActive(true);
                        
                        // Cập nhật dữ liệu hạng, tên, điểm chiến lực
                        newItem.UpdateData(rankCounter, entry.Nickname, entry.Value);
                        
                        activeItems.Add(newItem);
                        rankCounter++;
                    }
                }
                else
                {
                    string errMsg = response?.Base?.Message ?? "Lỗi không xác định từ server.";
                    Debug.LogError($"[Leaderboard] Lấy dữ liệu thất bại: {errMsg}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Leaderboard] Exception: {ex.Message}");
            }
        }

        private void ClearLeaderboard()
        {
            foreach (var item in activeItems)
            {
                if (item != null) Destroy(item.gameObject);
            }
            activeItems.Clear();
        }

        private void ClosePanel()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ClosePanel("UI_LeaderboardPanel");
            }
        }
    }
}
