using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Game.Core
{
    public sealed class DebugRandomEventHotkey : ITickable
    {
        private readonly EventBus _eventBus;
        private readonly DebugMode _debug;

        public DebugRandomEventHotkey(EventBus eventBus, DebugMode debug)
        {
            _eventBus = eventBus;
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

            if (keyboard.digit4Key.wasPressedThisFrame)
            {
                _eventBus.Publish(new RandomEventRequestedEvent());
            }
        }
    }
}
