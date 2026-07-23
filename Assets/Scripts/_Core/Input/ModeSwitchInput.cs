using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Game.Core
{
    // Слушает действие "Dock" в обеих активных картах (Flight и Platformer) и
    // публикует запрос на переключение режима. В каждый момент включена лишь
    // одна карта, поэтому реагирует только та, что соответствует текущему стейту.
    public sealed class ModeSwitchInput : ITickable
    {
        private readonly EventBus _eventBus;
        private readonly InputAction _flightDock;
        private readonly InputAction _platformerDock;

        public ModeSwitchInput(EventBus eventBus, InputActionAsset inputActions)
        {
            _eventBus = eventBus;
            _flightDock = inputActions.FindActionMap("Flight", throwIfNotFound: true)
                .FindAction("Dock", throwIfNotFound: true);
            _platformerDock = inputActions.FindActionMap("Platformer", throwIfNotFound: true)
                .FindAction("Dock", throwIfNotFound: true);
        }

        public void Tick()
        {
            if (_flightDock.WasPressedThisFrame() || _platformerDock.WasPressedThisFrame())
            {
                _eventBus.Publish(new ModeSwitchRequestedEvent());
            }
        }
    }
}
