using UnityEngine;

namespace Game.Core
{
    public sealed class BoardingState : IGameState
    {
        public string ActionMapName => "Combat";

        public void Enter() => Debug.Log($"[GameState] Enter {nameof(BoardingState)}");
        public void Exit() => Debug.Log($"[GameState] Exit {nameof(BoardingState)}");
    }
}
