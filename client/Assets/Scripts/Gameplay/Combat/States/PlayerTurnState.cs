using System.Collections;
using UnityEngine;

namespace GameClient.Gameplay.Combat.States
{
    public class PlayerTurnState : ICombatState
    {
        public IEnumerator Enter(CombatManager manager)
        {
            Debug.Log($"[Combat] Đến lượt người chơi: {manager.CurrentActiveEntity.entityName}");
            yield break;
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
