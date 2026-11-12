using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Game.Core
{
    // Дебаг: R — перегенерировать зону (очистить SpaceRoot.Content и создать
    // заново). Настоящий триггер придёт со Stage 14 (границы карты).
    public sealed class DebugRegenerateHotkey : ITickable
    {
        private readonly EventBus _eventBus;
        private readonly DebugMode _debug;

        public DebugRegenerateHotkey(EventBus eventBus, DebugMode debug)
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

            if (keyboard.rKey.wasPressedThisFrame)
            {
                _eventBus.Publish(new ZoneRegenerateRequestedEvent());
            }
        }
    }
}
