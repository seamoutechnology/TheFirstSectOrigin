using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using GameClient.Core;
using GameClient.Network;
using GameClient.Network.Pb;

namespace GameClient.Managers
{
    public class MissionManager : Singleton<MissionManager>
    {
        public List<Mission> Missions { get; private set; } = new List<Mission>();

        public event Action OnMissionsChanged;

        protected override void Awake()
        {
            base.Awake();
        }

        public async Task LoadMissionsAsync(MissionType type = MissionType.Daily)
        {
            try
            {
                if (NetworkManager.Instance == null || NetworkManager.Instance.GatewayClient == null)
                {
                    Debug.LogWarning("[MissionManager] NetworkManager hoặc GatewayClient chưa sẵn sàng!");
                    return;
                }

                var req = new GetMissionsRequest { FilterType = type };
                var res = await NetworkManager.Instance.GatewayClient.GetMissionsAsync(req, NetworkManager.DefaultCallOptions());

                if (res != null && res.Code == 0)
                {
                    Missions.Clear();
                    if (res.Missions != null)
                    {
                        Missions.AddRange(res.Missions);
                    }
                    Debug.Log($"[MissionManager] Đã tải {Missions.Count} nhiệm vụ thành công.");
                    OnMissionsChanged?.Invoke();
                }
                else
                {
                    Debug.LogWarning($"[MissionManager] Lỗi tải nhiệm vụ: {res?.Code}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionManager] Ngoại lệ khi tải nhiệm vụ: {ex.Message}");
            }
        }

        public async Task<bool> ClaimMissionRewardAsync(int missionId)
        {
            try
            {
                if (NetworkManager.Instance == null || NetworkManager.Instance.GatewayClient == null)
                {
                    Debug.LogWarning("[MissionManager] NetworkManager hoặc GatewayClient chưa sẵn sàng!");
                    return false;
                }

                var req = new ClaimMissionRewardRequest { MissionId = missionId };
                var res = await NetworkManager.Instance.GatewayClient.ClaimMissionRewardAsync(req, NetworkManager.DefaultCallOptions());

                if (res != null && res.Code == 0)
                {
                    Debug.Log($"[MissionManager] Nhận phần thưởng nhiệm vụ {missionId} thành công!");
                    
                    // Cập nhật trạng thái local của nhiệm vụ
                    var mission = Missions.Find(m => m.MissionId == missionId);
                    if (mission != null)
                    {
                        mission.Status = MissionStatus.Rewarded;
                    }

                    OnMissionsChanged?.Invoke();
                    
                    if (ToastManager.Instance != null)
                    {
                        ToastManager.Instance.ShowBigToast("Đã nhận phần thưởng nhiệm vụ!", 2f);
                    }

                    return true;
                }
                else
                {
                    Debug.LogWarning($"[MissionManager] Lỗi nhận thưởng: {res?.Code}");
                    if (ToastManager.Instance != null)
                    {
                        ToastManager.Instance.ShowBigToast("Nhận thưởng thất bại, vui lòng thử lại!", 2f);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionManager] Ngoại lệ khi nhận thưởng nhiệm vụ: {ex.Message}");
            }

            return false;
        }
    }
}
