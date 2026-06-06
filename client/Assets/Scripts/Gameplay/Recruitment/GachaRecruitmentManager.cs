using System.Collections.Generic;
using UnityEngine;
using GameClient.Managers;

namespace GameClient.Gameplay.Recruitment
{
    public class GachaRecruitCandidate
    {
        public string Name;
        public string Potential;
        public int Lifespan;
    }

    public class GachaRecruitmentManager : GameClient.Singleton<GachaRecruitmentManager>
    {
        public List<GachaRecruitCandidate> CurrentCandidates { get; private set; } = new List<GachaRecruitCandidate>();

        public void Summon(int cost)
        {
            var allDisciples = Object.FindObjectsByType<GameClient.BaseBuilding.AI.DiscipleAI>(FindObjectsSortMode.None);
            int currentCount = allDisciples.Length;
            int maxCapacity = GameContext.MaxDiscipleCapacity;

            if (currentCount + 3 > maxCapacity)
            {
                Debug.LogWarning($"[Gacha] Không đủ chỗ trống! Cần ít nhất 3 chỗ. Hiện có: {currentCount}/{maxCapacity}. Hãy nâng cấp tông môn hoặc đuổi bớt đệ tử.");
                return;
            }

            CurrentCandidates.Clear();
            Debug.Log($"[Gacha] Tiêu hao {cost} tài nguyên để triệu hồi...");

            for (int i = 0; i < 3; i++)
            {
                var candidate = new GachaRecruitCandidate
                {
                    Name = $"Tản Tu {Random.Range(100, 999)}",
                    Lifespan = Random.Range(50, 100)
                };

                float val = Random.value;
                if (val < 0.05f * GameContext.SectLevel)
                    candidate.Potential = "Thượng Phẩm";
                else if (val < 0.2f * GameContext.SectLevel)
                    candidate.Potential = "Trung Phẩm";
                else
                    candidate.Potential = "Hạ Phẩm";

                CurrentCandidates.Add(candidate);
                Debug.Log($"[Gacha] Sinh ra đệ tử: {candidate.Name} - {candidate.Potential} - Thọ nguyên: {candidate.Lifespan}");
            }
        }

        public void AcceptCandidate(GachaRecruitCandidate candidate)
        {
            if (!CurrentCandidates.Contains(candidate)) return;
            
            Debug.Log($"[Gacha] Chấp nhận đệ tử {candidate.Name} gia nhập Tông Môn.");
            
            CurrentCandidates.Remove(candidate);
        }

        public void RejectCandidate(GachaRecruitCandidate candidate)
        {
            if (!CurrentCandidates.Contains(candidate)) return;

            int refundAmount = 10;
            if (candidate.Potential == "Thượng Phẩm") refundAmount = 50;
            else if (candidate.Potential == "Trung Phẩm") refundAmount = 30;
            
            Debug.Log($"[Gacha] Từ chối đệ tử {candidate.Name} ({candidate.Potential}). Hoàn lại {refundAmount} tài nguyên.");
            
            CurrentCandidates.Remove(candidate);
        }
    }
}
