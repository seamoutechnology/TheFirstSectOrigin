using System.Collections.Generic;
using UnityEngine;

namespace TFSO.Core
{
    public interface IState
    {
        void OnEnter();
        void OnUpdate();
        void OnExit();
    }

    public class StateMachine
    {
        private IState _currentState;
        public IState CurrentState => _currentState;

        public void ChangeState(IState newState)
        {
            _currentState?.OnExit();
            _currentState = newState;
            _currentState?.OnEnter();
        }

        public void Update()
        {
            _currentState?.OnUpdate();
        }
    }
}
