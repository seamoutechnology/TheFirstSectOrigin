using System;
using UnityEngine;
using GameClient.Core;

namespace GameClient.Managers
{
    public class TimestampManager : Singleton<TimestampManager>
    {
        private long _serverUnixTimeAtSync;
        
        private float _clientRealtimeAtSync;

        private bool _isSynced = false;

        protected override void Awake()
        {
            base.Awake();

            SyncWithServer();
        }

        public void SyncWithServer()
        {
            // TODO: Ở game thật, lấy thông số này từ gói tin đăng nhập trả về.
            _serverUnixTimeAtSync = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _clientRealtimeAtSync = Time.realtimeSinceStartup;
            _isSynced = true;
            
            Debug.Log($"[TimestampManager] Đã đồng bộ giờ Server: {_serverUnixTimeAtSync}");
        }

        public long GetCurrentServerTimeMs()
        {
            if (!_isSynced)
            {
                return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            float elapsedTimeSec = Time.realtimeSinceStartup - _clientRealtimeAtSync;
            long elapsedTimeMs = (long)(elapsedTimeSec * 1000f);

            return _serverUnixTimeAtSync + elapsedTimeMs;
        }
    }
}
