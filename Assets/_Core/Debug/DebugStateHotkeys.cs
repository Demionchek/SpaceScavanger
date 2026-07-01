using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Game.Core
{
    public sealed class DebugStateHotkeys : ITickable
    {
        private readonly GameStateMachine _stateMachine;

        public DebugStateHotkeys(GameStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public void Tick()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                _stateMachine.ChangeState<ShipInteriorState>();
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                _stateMachine.ChangeState<SpaceFlightState>();
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                _stateMachine.ChangeState<BoardingState>();
            }
        }
    }
}
