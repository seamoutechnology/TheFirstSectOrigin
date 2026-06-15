using UnityEngine;
using TMPro;
using GameClient.Utils;

namespace GameClient.UI
{
    public class UI_LeaderboardItem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text txtRank;
        [SerializeField] private TMP_Text txtNickname;
        [SerializeField] private TMP_Text txtValue;

        public void UpdateData(int rank, string nickname, long value)
        {
            if (txtRank != null) txtRank.text = rank.ToString();
            if (txtNickname != null) txtNickname.text = nickname;
            if (txtValue != null) txtValue.text = NumberUtils.FormatNumber(value);

            // Top 3 ranking color overrides
            if (rank == 1 && txtRank != null) txtRank.color = Color.yellow;
            else if (rank == 2 && txtRank != null) txtRank.color = new Color(0.75f, 0.75f, 0.75f); // Silver
            else if (rank == 3 && txtRank != null) txtRank.color = new Color(0.8f, 0.5f, 0.2f);   // Bronze
            else if (txtRank != null) txtRank.color = Color.white;
        }
    }
}
