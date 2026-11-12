using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Game.Core
{
    public sealed class DebugStateHotkeys : ITickable
    {
        private readonly GameStateMachine _stateMachine;
        private readonly DebugMode _debug;

        public DebugStateHotkeys(GameStateMachine stateMachine, DebugMode debug)
        {
            _stateMachine = stateMachine;
            _debug = debug;
        }

        public void Tick()
        {
            if (!_debug.Enabled)
            {
                return;
            }

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
