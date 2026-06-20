using System;
using UnityEngine;

namespace IdleOff.Game
{
    [DisallowMultipleComponent]
    public sealed class GameStateManager : MonoBehaviour
    {
        public static GameStateManager Instance { get; private set; }
        public GameState CurrentState { get; private set; } = GameState.Boot;
        public event Action<GameState, GameState> StateChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                return;
            }

            if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void SetState(GameState nextState)
        {
            if (CurrentState == nextState)
            {
                return;
            }

            var previous = CurrentState;
            CurrentState = nextState;
            StateChanged?.Invoke(previous, nextState);
        }
    }
}
