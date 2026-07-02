using UnityEngine;

namespace Game.Core
{
    public sealed class SpaceFlightState : IGameState
    {
        public string ActionMapName => "Flight";

        public void Enter() => Debug.Log($"[GameState] Enter {nameof(SpaceFlightState)}");
        public void Exit() => Debug.Log($"[GameState] Exit {nameof(SpaceFlightState)}");
    }
}
