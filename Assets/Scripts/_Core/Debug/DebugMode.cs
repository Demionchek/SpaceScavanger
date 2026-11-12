using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Game.Core
{
    // Рубильник отладочных клавиш. По умолчанию они выключены: 1/2/3 меняли режим
    // игры посреди полёта, R пересобирал сектор, F9 откатывал сейв — нажать такое
    // случайно проще простого, а в чужих руках это просто сломанная игра.
    public sealed class DebugMode : ITickable
    {
        private const Key ToggleKey = Key.F1;

        private readonly EventBus _eventBus;

        public bool Enabled { get; private set; }

        public DebugMode(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Tick()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard[ToggleKey].wasPressedThisFrame)
            {
                return;
            }

            Enabled = !Enabled;

            // Баннер по центру: без ответа непонятно, включилось оно или нет.
            _eventBus.Publish(new WarningMessageEvent(
                Enabled ? "Debug keys ON" : "Debug keys OFF"));
        }
    }
}
