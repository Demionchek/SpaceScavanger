using UnityEngine;

namespace Game.Core
{
    public sealed class ShipInteriorState : IGameState
    {
        public string ActionMapName => "Platformer";

        public void Enter() => Debug.Log($"[GameState] Enter {nameof(ShipInteriorState)}");
        public void Exit() => Debug.Log($"[GameState] Exit {nameof(ShipInteriorState)}");
    }
}
