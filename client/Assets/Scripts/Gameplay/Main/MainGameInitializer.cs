using System.Collections;
using UnityEngine;
using GameClient.Managers;
using GameClient.Gameplay.BaseBuilder;

namespace GameClient.Gameplay.Main
{
    public class MainGameInitializer : MonoBehaviour
    {
        private void Start()
        {
            StartCoroutine(InitMainGameRoutine());
        }

        private IEnumerator InitMainGameRoutine()
        {
            Debug.Log("[MainGameInitializer] Bắt đầu đồng bộ dữ liệu Tông Môn từ Server...");

            yield return new WaitForSeconds(1.0f);

            GameContext.SectLevel = 5;
            GameContext.SectReputation = 1500;
            GameContext.SectAlignment = -80; // Tà Tu


            Debug.Log($"[MainGameInitializer] Đồng bộ hoàn tất! Level: {GameContext.SectLevel}, Uy danh: {GameContext.SectReputation}");

            GameContext.OnServerDataSynced?.Invoke();

        }
    }
}
