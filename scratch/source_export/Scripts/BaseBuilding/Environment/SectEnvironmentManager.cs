using UnityEngine;

namespace GameClient.BaseBuilding.Environment
{
    public class SectEnvironmentManager : MonoBehaviour
    {
        public enum TimeOfDay
        {
            Dawn,
            Day,
            Dusk,
            Night
        }

        [Header("Time Settings")]
        public float RealMinutesPerGameDay = 24f; // 24 phút ngoài đời = 1 ngày trong game
        [Range(0f, 1f)]
        public float CurrentTimeOfDay; 

        [Header("Lighting Colors")]
        public Color DawnColor = new Color(1f, 0.7f, 0.6f);
        public Color DayColor = new Color(1f, 1f, 1f);
        public Color DuskColor = new Color(1f, 0.4f, 0.2f);
        public Color NightColor = new Color(0.2f, 0.2f, 0.4f);

        private float _timeMultiplier;

        private void Start()
        {
            _timeMultiplier = 1f / (RealMinutesPerGameDay * 60f);
        }

        private void Update()
        {
            UpdateDayNightCycle();
            UpdateLighting();
        }

        private void UpdateDayNightCycle()
        {
            CurrentTimeOfDay += Time.deltaTime * _timeMultiplier;
            if (CurrentTimeOfDay >= 1f)
            {
                CurrentTimeOfDay = 0f; // Sang ngày mới
                OnNewDayStarted();
            }
        }

        private void OnNewDayStarted()
        {
            Debug.Log("[SectEnvironmentManager] A new day has started in the Sect!");
            
            var allDisciples = Object.FindObjectsByType<GameClient.BaseBuilding.AI.DiscipleAI>(FindObjectsSortMode.None);
            
            bool isEvil = GameClient.Managers.GameContext.IsEvil;

            foreach (var disciple in allDisciples)
            {
                disciple.ConsumeLifespan(1);

                int maxLoss = isEvil ? 4 : 2;
                int loyaltyLoss = Random.Range(0, maxLoss); 
                disciple.ChangeLoyalty(-loyaltyLoss);
            }

            TryRecruitDisciple();
            
            if (isEvil && Random.value < 0.1f)
            {
                Debug.LogWarning("[SectEnvironmentManager] Tông môn (Tà Tu) đang bị các thế lực khác tấn công!");
                // TODO: Sinh ra quái hoặc NPC tấn công tông môn
            }
        }

        public int DisciplesWaitingAtGate { get; private set; } = 0;

        private void TryRecruitDisciple()
        {
            var allDisciples = Object.FindObjectsByType<GameClient.BaseBuilding.AI.DiscipleAI>(FindObjectsSortMode.None);
            
            int rep = GameClient.Managers.GameContext.SectReputation;
            float chance = Mathf.Clamp01(rep / 1000f + 0.1f); 
            
            if (Random.value < chance)
            {
                if (allDisciples.Length + DisciplesWaitingAtGate >= GameClient.Managers.GameContext.MaxDiscipleCapacity)
                {
                    Debug.Log("[SectEnvironmentManager] Tông môn đã đạt giới hạn đệ tử tối đa, tản tu mới đến đành bỏ đi.");
                    return;
                }

                if (DisciplesWaitingAtGate >= GameClient.Managers.GameContext.MaxWaitingQueue)
                {
                    Debug.Log($"[SectEnvironmentManager] Hàng chờ ngoài cổng đã chật cứng ({DisciplesWaitingAtGate}/{GameClient.Managers.GameContext.MaxWaitingQueue}), tản tu chán nản bỏ đi.");
                    return;
                }

                DisciplesWaitingAtGate++;

                string potential = "Hạ Phẩm";
                if (rep > 500 && Random.value < 0.3f) potential = "Trung Phẩm";
                if (rep > 1000 && Random.value < 0.1f) potential = "Thượng Phẩm";

                Debug.Log($"[SectEnvironmentManager] Có một tản tu ({potential}) đến xin gia nhập! (Đang chờ ở cổng: {DisciplesWaitingAtGate}/{GameClient.Managers.GameContext.MaxWaitingQueue})");
                // TODO: Người chơi phải click vào cổng tông môn để duyệt (Accept/Reject) đệ tử trong hàng chờ
            }
        }

        private void UpdateLighting()
        {
            
            Color targetColor;
            
            if (CurrentTimeOfDay < 0.25f)
            {
                float t = CurrentTimeOfDay / 0.25f;
                targetColor = Color.Lerp(DawnColor, DayColor, t);
            }
            else if (CurrentTimeOfDay < 0.5f)
            {
                float t = (CurrentTimeOfDay - 0.25f) / 0.25f;
                targetColor = Color.Lerp(DayColor, DuskColor, t);
            }
            else if (CurrentTimeOfDay < 0.75f)
            {
                float t = (CurrentTimeOfDay - 0.5f) / 0.25f;
                targetColor = Color.Lerp(DuskColor, NightColor, t);
            }
            else
            {
                float t = (CurrentTimeOfDay - 0.75f) / 0.25f;
                targetColor = Color.Lerp(NightColor, DawnColor, t);
            }

            if (Camera.main != null && Camera.main.clearFlags == CameraClearFlags.SolidColor)
            {
                Camera.main.backgroundColor = targetColor;
            }
        }
        
        public TimeOfDay GetTimeOfDay()
        {
            if (CurrentTimeOfDay < 0.25f) return TimeOfDay.Dawn;
            if (CurrentTimeOfDay < 0.5f) return TimeOfDay.Day;
            if (CurrentTimeOfDay < 0.75f) return TimeOfDay.Dusk;
            return TimeOfDay.Night;
        }
    }
}
