using System.Collections;
using System.Linq;
using UnityEngine;
using GameClient.Network;
using GameClient.Network.Pb;

namespace GameClient.Gameplay.Combat.States
{
    public class GameOverState : ICombatState
    {
        public IEnumerator Enter(CombatManager manager)
        {
            bool allEnemiesDead = manager.Enemies.All(e => e.IsDead);

            if (allEnemiesDead)
            {
                Debug.Log("<color=green>[Combat] CHIẾN THẮNG! Gửi kết quả lên server...</color>");
            }
            else
            {
                Debug.Log("<color=red>[Combat] THẤT BẠI! Gửi kết quả lên server...</color>");
            }

            var req = new ValidatePvEResultRequest
            {
                EnemyId = manager.EnemyID,
                IsVictory = allEnemiesDead,
                PlayerPower = manager.PlayerTotalPower,
                EnemyPower = manager.EnemyTotalPower,
            };
            req.CombatLogs.AddRange(manager.CombatLogs);

            var call = NetworkManager.Instance.GatewayClient.ValidatePvEResultAsync(req);
            call.ResponseAsync.ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"[Combat] Lỗi kết nối Server: {task.Exception}");
                }
                else
                {
                    var resp = task.Result;
                    if (resp.IsValid)
                    {
                        Debug.Log($"<color=cyan>[Combat] Server xác nhận Hợp lệ! Thưởng: {resp.RewardExp} Exp, {resp.RewardLinhThach} LT.</color>");
                    }
                    else
                    {
                        Debug.LogError($"[Combat] Server từ chối kết quả: {resp.Base.Message}");
                    }
                }
            });

            manager.OnCombatEnded?.Invoke();
            yield break;
        }

        public void Execute(CombatManager manager)
        {
        }

        public IEnumerator Exit(CombatManager manager)
        {
            yield break;
        }
    }
}
