using System.Collections;
using UnityEngine;

namespace GameClient.Gameplay.Combat.States
{
    public class InitState : ICombatState
    {
        public IEnumerator Enter(CombatManager manager)
        {
            Debug.Log("[Combat] Bắt đầu trận chiến!");
            manager.DetermineTurnOrder();
            yield return new WaitForSeconds(1f);
            manager.NextTurn();
        }

        public void Execute(CombatManager manager)
        {
        }

        public IEnumerator Exit(CombatManager manager)
        {
            yield break;
        }
    }
}
