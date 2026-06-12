using System.Collections;
using System.Linq;
using UnityEngine;
using GameClient.Network;
using GameClient.Network.Pb;
using GameClient.UI;
using GameClient.UI.Combat;

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
                if (CombatStartData.CurrentStage != null && !string.IsNullOrEmpty(CombatStartData.CurrentStage.stageId))
                {
                    string completed = PlayerPrefs.GetString("CompletedStages", "");
                    var completedList = completed.Split(',').Where(s => !string.IsNullOrEmpty(s)).ToList();
                    if (!completedList.Contains(CombatStartData.CurrentStage.stageId))
                    {
                        completedList.Add(CombatStartData.CurrentStage.stageId);
                        PlayerPrefs.SetString("CompletedStages", string.Join(",", completedList));
                        PlayerPrefs.Save();
                        Debug.Log($"[Combat] Đã lưu tiến trình vượt ải: {CombatStartData.CurrentStage.stageId}");
                    }
                }
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

            var call = NetworkManager.Instance.GatewayClient.ValidatePvEResultAsync(req, NetworkManager.DefaultCallOptions());
            
            // Safe yielding on main thread to await gRPC call completion
            var task = call.ResponseAsync;
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                Debug.LogError($"[Combat] Lỗi kết nối Server: {task.Exception}");
                // Fallback local response representation so that client does not get stuck
                if (allEnemiesDead)
                {
                    UIManager.Instance.OpenPanel("CombatVictoryPanel", new CombatResultPanel.LocalResultData
                    {
                        IsVictory = true,
                        RewardExp = 100,
                        RewardLinhThach = 50
                    });
                }
                else
                {
                    UIManager.Instance.OpenPanel("CombatDefeatPanel");
                }
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
                    Debug.LogError($"[Combat] Server từ chối kết quả: {resp.Base?.Message}");
                }
                
                if (allEnemiesDead)
                {
                    UIManager.Instance.OpenPanel("CombatVictoryPanel", resp);
                }
                else
                {
                    UIManager.Instance.OpenPanel("CombatDefeatPanel", resp);
                }
            }

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
