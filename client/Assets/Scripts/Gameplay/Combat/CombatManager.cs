using UnityEngine;
using System.Collections.Generic;
using GameClient.Gameplay.Combat.States;
using GameClient.Gameplay.Combat.Skills;
using System.Linq;
using System;
using GameClient.Network.Pb;

namespace GameClient.Gameplay.Combat
{
    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance { get; private set; }

        public CombatStateMachine StateMachine { get; private set; }

        public List<CombatEntity> Players = new List<CombatEntity>();
        public List<CombatEntity> Enemies = new List<CombatEntity>();
        
        public List<CombatEntity> TurnQueue = new List<CombatEntity>();
        public CombatEntity CurrentActiveEntity { get; private set; }

        public string EnemyID = "monster_001"; // TODO: set appropriately
        public int PlayerTotalPower;
        public int EnemyTotalPower;

        public List<CombatActionLog> CombatLogs = new List<CombatActionLog>();

        public Action<CombatEntity> OnTurnStarted;
        public Action OnCombatEnded;

        public SkillData SelectedSkill;
        public CombatEntity SelectedTarget;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            StateMachine = gameObject.AddComponent<CombatStateMachine>();
            StateMachine.Initialize(this);
        }

        public void StartCombat(List<CombatEntity> players, List<CombatEntity> enemies)
        {
            Players = players;
            Enemies = enemies;

            PlayerTotalPower = players.Sum(p => p.attack + p.defense + p.maxHP);
            EnemyTotalPower = enemies.Sum(e => e.attack + e.defense + e.maxHP);

            CombatLogs.Clear();

            StateMachine.ChangeState(new InitState());
        }

        public void DetermineTurnOrder()
        {
            TurnQueue.Clear();
            TurnQueue.AddRange(Players.Where(p => !p.IsDead));
            TurnQueue.AddRange(Enemies.Where(e => !e.IsDead));

            TurnQueue.Sort((a, b) => b.speed.CompareTo(a.speed));
        }

        public void NextTurn()
        {
            if (CheckGameOver())
            {
                StateMachine.ChangeState(new GameOverState());
                return;
            }

            if (TurnQueue.Count == 0)
            {
                DetermineTurnOrder();
            }

            if (TurnQueue.Count > 0)
            {
                CurrentActiveEntity = TurnQueue[0];
                TurnQueue.RemoveAt(0);

                if (CurrentActiveEntity.IsDead)
                {
                    NextTurn();
                    return;
                }

                OnTurnStarted?.Invoke(CurrentActiveEntity);

                if (CurrentActiveEntity.isPlayer)
                {
                    StateMachine.ChangeState(new PlayerTurnState());
                }
                else
                {
                    StateMachine.ChangeState(new EnemyTurnState());
                }
            }
        }

        public bool CheckGameOver()
        {
            bool allPlayersDead = Players.All(p => p.IsDead);
            bool allEnemiesDead = Enemies.All(e => e.IsDead);
            return allPlayersDead || allEnemiesDead;
        }

        public void ExecuteAction(SkillData skill, CombatEntity target)
        {
            SelectedSkill = skill;
            SelectedTarget = target;
            StateMachine.ChangeState(new ActionExecutionState());
        }
    }
}
