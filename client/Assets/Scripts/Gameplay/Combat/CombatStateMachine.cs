using UnityEngine;
using System.Collections;
using GameClient.Gameplay.Combat.States;

namespace GameClient.Gameplay.Combat
{
    public class CombatStateMachine : MonoBehaviour
    {
        private ICombatState _currentState;
        private CombatManager _combatManager;
        private bool _isTransitioning;

        public void Initialize(CombatManager manager)
        {
            _combatManager = manager;
        }

        public void ChangeState(ICombatState newState)
        {
            if (_isTransitioning) return;
            StartCoroutine(ChangeStateRoutine(newState));
        }

        private IEnumerator ChangeStateRoutine(ICombatState newState)
        {
            _isTransitioning = true;

            if (_currentState != null)
            {
                yield return StartCoroutine(_currentState.Exit(_combatManager));
            }

            _currentState = newState;
            Debug.Log($"[CombatStateMachine] Chuyển sang trạng thái: {_currentState.GetType().Name}");
            
            yield return StartCoroutine(_currentState.Enter(_combatManager));
            
            _isTransitioning = false;
        }

        private void Update()
        {
            if (!_isTransitioning && _currentState != null)
            {
                _currentState.Execute(_combatManager);
            }
        }
    }
}
