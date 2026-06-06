using System.Collections.Generic;
using UnityEngine;

namespace GameClient.Gameplay.BaseBuilder
{
    public class HarvestSyncManager : MonoBehaviour
    {
        public static HarvestSyncManager Instance { get; private set; }

        private HashSet<string> _pendingHarvests = new HashSet<string>();
        private float _syncTimer = 0f;
        private const float SYNC_INTERVAL = 60f; // 1 Minute

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void QueueHarvest(string buildingId)
        {
            if (!_pendingHarvests.Contains(buildingId))
            {
                _pendingHarvests.Add(buildingId);
                Debug.Log($"[HarvestSync] Thêm vào hàng đợi thu hoạch: {buildingId}");
            }
        }

        private void Update()
        {
            if (_pendingHarvests.Count > 0)
            {
                _syncTimer += Time.deltaTime;
                if (_syncTimer >= SYNC_INTERVAL)
                {
                    SyncWithServer();
                }
            }
        }

        public void ForceSync()
        {
            if (_pendingHarvests.Count > 0)
            {
                SyncWithServer();
            }
        }

        private void SyncWithServer()
        {
            _syncTimer = 0f;
            
            List<string> batch = new List<string>(_pendingHarvests);
            _pendingHarvests.Clear();

            Debug.Log($"[HarvestSync] Đang gửi Batch Request thu hoạch lên Server... Số lượng: {batch.Count}");


            int mockResult = 0; // Giả sử Server trả về 0 (Thành công)

            if (mockResult == 1)
            {
                Debug.LogWarning("[HarvestSync] Lệch đồng bộ! Gọi API tải lại Map...");
            }
            else if (mockResult == 2)
            {
                Debug.LogError("[HarvestSync] PHÁT HIỆN GIAN LẬN!!! Server đóng kết nối.");
                Application.Quit();
            }
            else
            {
                Debug.Log("[HarvestSync] Sync thành công! Đồ đã thực sự vào túi ở DB.");
            }
        }
    }
}
