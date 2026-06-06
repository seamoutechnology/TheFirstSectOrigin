using UnityEngine;
using GameClient.Core;

namespace GameClient.WorldMap
{
    public class WorldMapController : Singleton<WorldMapController>
    {
        [Header("World Settings")]
        public Vector2 mapBoundsMin = new Vector2(-1000, -1000);
        public Vector2 mapBoundsMax = new Vector2(1000, 1000);

        private void Start()
        {
            Debug.Log("[WorldMapController] Khởi tạo Bản đồ Thế giới...");
            // TODO: Fetch data từ Server về các mỏ tài nguyên, base của người chơi khác
        }

        public bool IsPositionInBounds(Vector2 pos)
        {
            return pos.x >= mapBoundsMin.x && pos.x <= mapBoundsMax.x &&
                   pos.y >= mapBoundsMin.y && pos.y <= mapBoundsMax.y;
        }

        public void SpawnWorldEntity(string entityId, Vector2 worldPos)
        {
            if (!IsPositionInBounds(worldPos))
            {
                Debug.LogWarning("[WorldMap] Tọa độ vượt quá giới hạn bản đồ!");
                return;
            }

            // TODO: Khởi tạo object thực thể từ Prefab
            Debug.Log($"[WorldMap] Spawn Entity {entityId} tại {worldPos}");
        }
    }
}
