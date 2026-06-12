using UnityEngine;
using UnityEngine.UI;
using GameClient.UI.Core;
using GameClient.Managers;

namespace GameClient.UI.Combat
{
    public class CombatDefeatPanel : BaseUIPanel
    {
        [Header("Buttons")]
        [SerializeField] private Button btnRetry;
        [SerializeField] private Button btnExit;

        protected override void OnInit()
        {
            base.OnInit();
            btnRetry.onClick.AddListener(OnRetryClicked);
            btnExit.onClick.AddListener(OnExitClicked);
        }

        private void OnRetryClicked()
        {
            Hide();
            Debug.Log("[CombatDefeatPanel] Retrying combat: reloading Dungeon scene.");
            if (MapManager.Instance != null)
            {
                _ = MapManager.Instance.LoadMapAsync(MapType.Dungeon);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("Dungeon");
            }
        }

        private void OnExitClicked()
        {
            Hide();
            Debug.Log("[CombatDefeatPanel] Returning to main base (LocalBase) to upgrade.");
            if (MapManager.Instance != null)
            {
                _ = MapManager.Instance.LoadMapAsync(MapType.LocalBase);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("LocalBase");
            }
        }
    }
}
