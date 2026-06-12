using UnityEngine;
using GameClient.Network.Api;
using GameClient.Network.Pb;
using System.Threading.Tasks;

namespace GameClient.Gameplay.Heroes
{
    public class GachaTester : MonoBehaviour
    {
        [Header("Gacha Test Settings")]
        [Tooltip("ID của Banner, nếu để -1 sẽ tự lấy banner đầu tiên tìm thấy trên server")]
        public int bannerId = -1;

        [ContextMenu("Gacha 1 Pull (1 Lượt)")]
        public void PullOne()
        {
            _ = DoPullAsync(1);
        }

        [ContextMenu("Gacha 10 Pulls (10 Lượt)")]
        public void PullTen()
        {
            _ = DoPullAsync(10);
        }

        private async Task DoPullAsync(int count)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("[GachaTester] Bạn phải chạy game (Play Mode) và đăng nhập mới có thể test Gacha.");
                return;
            }

            int targetBannerId = bannerId;
            if (targetBannerId == -1)
            {
                Debug.Log("[GachaTester] Đang kiểm tra danh sách banner trên server...");
                try
                {
                    var bannerResp = await GachaApi.GetGachaBannersAsync();
                    if (bannerResp.Base.Code != 0 || bannerResp.Banners.Count == 0)
                    {
                        Debug.LogError($"[GachaTester] Không tìm thấy banner nào. Code: {bannerResp.Base.Code}, Msg: {bannerResp.Base.Message}");
                        return;
                    }
                    targetBannerId = bannerResp.Banners[0].BannerId;
                    Debug.Log($"[GachaTester] Chọn Banner mặc định: {bannerResp.Banners[0].Name} (ID: {targetBannerId})");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[GachaTester] Lỗi lấy banner: {ex.Message}");
                    return;
                }
            }

            Debug.Log($"[GachaTester] Đang thực hiện triệu hồi {count} lượt trên Banner ID: {targetBannerId}...");
            try
            {
                var resp = await GachaApi.DoGachaAsync(targetBannerId, count);
                if (resp.Base.Code == 0)
                {
                    // Update Player in GM
                    if (GameManager.Instance != null && resp.PlayerAfter != null)
                    {
                        GameManager.Instance.SetPlayer(resp.PlayerAfter);
                    }

                    Debug.Log($"<color=green>[GachaTester] Gacha thành công! Nhận được {resp.Heroes.Count} tướng:</color>");
                    foreach (var h in resp.Heroes)
                    {
                        Debug.Log($"  - [Rarity: {h.Rarity}] Name: {h.Name} (ID: {h.Id}, Power: {h.Power})");
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.PlayerHeroes.Add(h);
                        }
                    }

                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.SetHeroes(GameManager.Instance.PlayerHeroes);
                    }

                    if (resp.PlayerAfter != null)
                    {
                        Debug.Log($"[GachaTester] Kim cương sau khi Gacha: {resp.PlayerAfter.Diamond}");
                    }
                }
                else
                {
                    Debug.LogError($"[GachaTester] Triệu hồi thất bại! Code: {resp.Base.Code}, Message: {resp.Base.Message}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[GachaTester] Lỗi triệu hồi Gacha: {ex.Message}");
            }
        }
    }
}
