using UnityEngine;
using System.Collections;
using GameClient.Gameplay.Combat.States;

namespace GameClient.Gameplay.Combat
{
    public class CombatStateMachine : MonoBehaviour
    {
        private ICombatState _currentState;
        private CombatManager _combatManager;
        public ICombatState CurrentState => _currentState;

        public void Initialize(CombatManager manager)
        {
            _combatManager = manager;
        }

        private Coroutine _stateCoroutine;

        public void ChangeState(ICombatState newState)
        {
            if (_stateCoroutine != null)
            {
                StopCoroutine(_stateCoroutine);
            }
            _stateCoroutine = StartCoroutine(ChangeStateRoutine(newState));
        }

        private IEnumerator ChangeStateRoutine(ICombatState newState)
        {
            if (_currentState != null)
            {
                yield return StartCoroutine(_currentState.Exit(_combatManager));
            }

            _currentState = newState;
            Debug.Log($"[CombatStateMachine] Chuyển sang trạng thái: {_currentState.GetType().Name}");
            
            yield return StartCoroutine(_currentState.Enter(_combatManager));
        }

        private void Update()
        {
            if (_currentState != null)
            {
                _currentState.Execute(_combatManager);
            }
        }
    }
}
