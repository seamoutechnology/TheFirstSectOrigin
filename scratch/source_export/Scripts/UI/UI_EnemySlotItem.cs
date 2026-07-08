using UnityEngine;
using TMPro;

namespace GameClient.UI
{
    public class UI_EnemySlotItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text txtName;

        public void Setup(string monsterName, bool isBoss, int level)
        {
            if (txtName != null)
            {
                txtName.text = monsterName + (isBoss ? " [BOSS]" : $" (Lv.{level})");
            }
        }
    }
}
