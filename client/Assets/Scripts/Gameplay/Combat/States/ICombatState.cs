using System.Collections;

namespace GameClient.Gameplay.Combat.States
{
    public interface ICombatState
    {
        IEnumerator Enter(CombatManager manager);
        void Execute(CombatManager manager);
        IEnumerator Exit(CombatManager manager);
    }
}
