using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Game.Core
{
    public sealed class DebugSaveHotkey : ITickable
    {
        private readonly EventBus _eventBus;

        public DebugSaveHotkey(EventBus eventBus)
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
