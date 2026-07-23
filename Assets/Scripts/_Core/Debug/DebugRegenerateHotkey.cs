using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Game.Core
{
    // Дебаг: R — перегенерировать зону (очистить SpaceRoot.Content и создать
    // заново). Настоящий триггер придёт со Stage 14 (границы карты).
    public sealed class DebugRegenerateHotkey : ITickable
    {
        private readonly EventBus _eventBus;

        public DebugRegenerateHotkey(EventBus eventBus)
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

            if (keyboard.rKey.wasPressedThisFrame)
            {
                _eventBus.Publish(new ZoneRegenerateRequestedEvent());
            }
        }
    }
}
