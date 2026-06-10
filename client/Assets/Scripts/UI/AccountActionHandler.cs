using UnityEngine;
using GameClient.Network;
using GameClient.Managers;

namespace GameClient.UI
{
    public class AccountActionHandler : MonoBehaviour
    {
        public void OnAccountIconActivated()
        {
            bool isLoggedIn = !string.IsNullOrEmpty(AccountManager.Instance.CurrentToken);

            if (isLoggedIn)
            {
                Debug.Log("[Account] Mở Mini-App Dashboard...");
                UIManager.Instance.OpenPanel("AccountDashboardPanel");
            }
            else
            {
                UIManager.Instance.OpenPanel("LoginPanel", null, false);
            }
        }
    }
}
