using UnityEngine;
using GameClient.Core;
using System;

namespace GameClient.Managers
{
    public enum GameState
    {
        None,
        Bootstrap,
        Login,
        Lobby,
        Battle
    }

    public class GameStateManager : Singleton<GameStateManager>
    {
        public GameState CurrentState { get; private set; } = GameState.None;
        public event Action<GameState, GameState> OnStateChanged; // (OldState, NewState)

        public void ChangeState(GameState newState)
        {
            if (CurrentState == newState) return;

            GameState oldState = CurrentState;
            CurrentState = newState;

            Debug.Log($"[GameState] Chuyển từ {oldState} sang {newState}");

            HandleStateExit(oldState);
            HandleStateEnter(newState);

            OnStateChanged?.Invoke(oldState, newState);
        }

        private void HandleStateEnter(GameState state)
        {
            switch (state)
            {
                case GameState.Login:
                    break;
                case GameState.Lobby:
                    break;
                case GameState.Battle:
                    break;
            }
        }

        private void HandleStateExit(GameState state)
        {
            switch (state)
            {
                case GameState.Battle:
                    break;
            }
        }
    }
}
