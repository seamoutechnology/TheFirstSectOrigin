using UnityEngine;
using GameClient.Managers;

namespace GameClient.Gameplay
{
    public class MainGameManager : MonoBehaviour
    {
        void Start()
        {
            Debug.Log($"[MainGameManager] Đã vào MainGame. Zone: {GameContext.CurrentServerName}");

            GameStateManager.Instance.ChangeState(GameState.Lobby);

            if (GameContext.HasCharacter)
            {
                Debug.Log("[MainGameManager] Người chơi ĐÃ có nhân vật. Bắt đầu load Gameplay...");
                StartGameplay();
            }
            else
            {
                Debug.Log("[MainGameManager] Người chơi CHƯA có nhân vật. Chạy Cutscene tạo nhân vật...");
                if (CutsceneManager.Instance != null)
                {
                    CutsceneManager.Instance.PlayCutscene("IntroCreation");
                }
                else
                {
                    Debug.LogError("[MainGameManager] Thiếu CutsceneManager trên Scene!");
                }
            }
        }

        private void StartGameplay()
        {
            UIManager.Instance.ShowMessage("Thông báo", "Chào mừng trở lại thế giới Tu Tiên!");
        }
    }
}
