using UnityEngine;

namespace TFSO.Interfaces
{
    public interface IDamageable
    {
        void TakeDamage(int amount);
        bool IsDead { get; }
    }

    public interface ISkill
    {
        string Name { get; }
        float Cooldown { get; }
        void Execute(GameObject caster, GameObject target);
    }

    public interface IUsable
    {
        void Use();
        string Description { get; }
    }

    public interface ITickable
    {
        void Tick(float deltaTime);
    }
}
