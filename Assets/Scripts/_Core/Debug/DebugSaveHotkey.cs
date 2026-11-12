using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Game.Core
{
    public sealed class DebugSaveHotkey : ITickable
    {
        private readonly EventBus _eventBus;
        private readonly DebugMode _debug;

        public DebugSaveHotkey(EventBus eventBus, DebugMode debug)
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

            if (keyboard.f5Key.wasPressedThisFrame)
            {
                _eventBus.Publish(new SaveGameRequestedEvent());
            }
            else if (keyboard.f9Key.wasPressedThisFrame)
            {
                _eventBus.Publish(new LoadGameRequestedEvent());
            }
        }
    }
}
