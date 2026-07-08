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

            // Try detecting using ButtonGroup child indices first (Retry is 1st, Exit is 2nd)
            var btnGroup = transform.Find("ButtonGroup");
            if (btnGroup != null && btnGroup.childCount >= 2)
            {
                if (btnRetry == null) btnRetry = btnGroup.GetChild(0).GetComponent<Button>();
                if (btnExit == null) btnExit = btnGroup.GetChild(1).GetComponent<Button>();
            }

            // Auto-detect btnRetry fallbacks
            if (btnRetry == null) btnRetry = transform.Find("btnRetry")?.GetComponent<Button>();
            if (btnRetry == null) btnRetry = transform.Find("Buttons/btnRetry")?.GetComponent<Button>();
            if (btnRetry == null)
            {
                foreach (var b in GetComponentsInChildren<Button>(true))
                {
                    string nameLower = b.name.ToLower();
                    if (nameLower.Contains("retry") || nameLower.Contains("lai"))
                    {
                        btnRetry = b;
                        break;
                    }
                }
            }
            if (btnRetry == null)
            {
                foreach (var b in GetComponentsInChildren<Button>(true))
                {
                    var textComp = b.GetComponentInChildren<TMPro.TMP_Text>();
                    if (textComp != null)
                    {
                        string txtLower = textComp.text.ToLower();
                        if (txtLower.Contains("retry") || txtLower.Contains("lại") || txtLower.Contains("lai") || txtLower.Contains("thử"))
                        {
                            btnRetry = b;
                            break;
                        }
                    }
                }
            }

            // Auto-detect btnExit fallbacks
            if (btnExit == null) btnExit = transform.Find("btnExit")?.GetComponent<Button>();
            if (btnExit == null) btnExit = transform.Find("Buttons/btnExit")?.GetComponent<Button>();
            if (btnExit == null)
            {
                foreach (var b in GetComponentsInChildren<Button>(true))
                {
                    string nameLower = b.name.ToLower();
                    if (nameLower.Contains("exit") || nameLower.Contains("quit") || nameLower.Contains("thoat") || nameLower.Contains("menu") || nameLower.Contains("base"))
                    {
                        btnExit = b;
                        break;
                    }
                }
            }
            if (btnExit == null)
            {
                foreach (var b in GetComponentsInChildren<Button>(true))
                {
                    var textComp = b.GetComponentInChildren<TMPro.TMP_Text>();
                    if (textComp != null)
                    {
                        string txtLower = textComp.text.ToLower();
                        if (txtLower.Contains("exit") || txtLower.Contains("quit") || txtLower.Contains("thoát") || txtLower.Contains("thoat") || txtLower.Contains("rời") || txtLower.Contains("roi"))
                        {
                            btnExit = b;
                            break;
                        }
                    }
                }
            }

            if (btnRetry != null) btnRetry.onClick.AddListener(OnRetryClicked);
            else Debug.LogWarning("[CombatDefeatPanel] btnRetry is not assigned and could not be auto-detected.");

            if (btnExit != null) btnExit.onClick.AddListener(OnExitClicked);
            else Debug.LogWarning("[CombatDefeatPanel] btnExit is not assigned and could not be auto-detected.");
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
