using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Game.Core
{
    public sealed class DebugRandomEventHotkey : ITickable
    {
        private readonly EventBus _eventBus;

        public DebugRandomEventHotkey(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Tick()
        {
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
