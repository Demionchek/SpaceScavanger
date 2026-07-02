using Game.Core;
using UnityEngine.InputSystem;

namespace Game.Gameplay.Flight
{
    public sealed class PlayerShipInput : IShipInputProvider
    {
        private readonly InputAction _throttleAction;
        private readonly InputAction _rotationAction;
        private readonly InputAction _fireAction;
        private readonly InputAction _interactAction;

        public PlayerShipInput(InputActionAsset inputActions)
        {
            var flightMap = inputActions.FindActionMap("Flight", throwIfNotFound: true);
            _throttleAction = flightMap.FindAction("Throttle", throwIfNotFound: true);
            _rotationAction = flightMap.FindAction("Rotation", throwIfNotFound: true);
            _fireAction = flightMap.FindAction("Fire", throwIfNotFound: true);
            _interactAction = flightMap.FindAction("Interact", throwIfNotFound: true);
        }

        public float Throttle => _throttleAction.ReadValue<float>();
        public float Rotation => _rotationAction.ReadValue<float>();
        public bool FirePressed => _fireAction.IsPressed();
        public bool InteractPressed => _interactAction.WasPressedThisFrame();
    }
}
