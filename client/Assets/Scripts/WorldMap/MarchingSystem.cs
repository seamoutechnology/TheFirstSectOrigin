using UnityEngine;
using DG.Tweening;

namespace GameClient.WorldMap
{
    public class MarchingSystem : MonoBehaviour
    {
        public static MarchingSystem Instance { get; private set; }

        public GameObject armyPrefab; // Prefab đội quân đại diện (có thể là con ngựa / tướng)
        public float defaultSpeed = 5f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void DispatchArmy(Vector2 startPos, Vector2 targetPos, string generalId)
        {
            if (armyPrefab == null)
            {
                Debug.LogError("[MarchingSystem] Chưa gán Army Prefab!");
                return;
            }

            GameObject armyObj = Instantiate(armyPrefab, startPos, Quaternion.identity);
            armyObj.name = $"Army_{generalId}";

            float distance = Vector2.Distance(startPos, targetPos);
            float duration = distance / defaultSpeed;

            Debug.Log($"[MarchingSystem] Tướng {generalId} xuất phát. Cần {duration:F1}s để đến nơi.");

            armyObj.transform.DOMove(targetPos, duration)
                .SetEase(Ease.Linear) // Di chuyển đều
                .OnComplete(() =>
                {
                    OnArmyArrived(armyObj, generalId, targetPos);
                });
        }

        private void OnArmyArrived(GameObject armyObj, string generalId, Vector2 pos)
        {
            Debug.Log($"[MarchingSystem] Đội quân của {generalId} đã đến {pos}! Kích hoạt sự kiện...");
            
            // TODO: Gửi request lên Server báo cáo đã đến nơi, hoặc kích hoạt Battle state
            
            Destroy(armyObj, 2f); // Tạm thời hủy sau 2s
        }
    }
}
