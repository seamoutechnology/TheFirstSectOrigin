using UnityEngine;
using System;
using System.Collections.Generic;

namespace GameClient.Gameplay.Combat
{
    public class CombatEntity : MonoBehaviour
    {
        public string entityName;
        public bool isPlayer;

        public int maxHP = 1000;
        public int currentHP;
        public int maxMP = 200;
        public int currentMP;

        public int attack = 100;
        public int defense = 50;
        public int speed = 10;

        public bool IsDead => currentHP <= 0;

        public Action<int, bool> OnTakeDamage; // amount, isCrit
        public Action OnDie;
        public Action<int> OnHealed;
        public Action<int> OnMPChanged;

        private void Start()
        {
            currentHP = maxHP;
            currentMP = maxMP;
        }

        public void TakeDamage(int rawDamage, bool isCrit)
        {
            if (IsDead) return;

            int actualDamage = Mathf.Max(1, rawDamage - defense);
            currentHP -= actualDamage;
            if (currentHP < 0) currentHP = 0;

            OnTakeDamage?.Invoke(actualDamage, isCrit);

            if (currentHP <= 0)
            {
                Die();
            }
        }

        public void Heal(int amount)
        {
            if (IsDead) return;

            currentHP += amount;
            if (currentHP > maxHP) currentHP = maxHP;

            OnHealed?.Invoke(amount);
        }

        public void AddMP(int amount)
        {
            if (IsDead) return;

            currentMP += amount;
            if (currentMP > maxMP) currentMP = maxMP;

            OnMPChanged?.Invoke(amount);
        }

        public void ConsumeMP(int amount)
        {
            currentMP -= amount;
            if (currentMP < 0) currentMP = 0;

            OnMPChanged?.Invoke(-amount);
        }

        private void Die()
        {
            OnDie?.Invoke();
        }

        public void Revive(int healAmount)
        {
            if (!IsDead) return;
            
            currentHP = healAmount;
            if (currentHP > maxHP) currentHP = maxHP;
            
            OnHealed?.Invoke(healAmount);
        }
    }
}
