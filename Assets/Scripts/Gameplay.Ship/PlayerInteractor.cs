using Game.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Game.Gameplay.Ship
{
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float _interactRadius = 1f;
        [SerializeField] private Transform _interactPoint;
        [SerializeField] private LayerMask _interactableMask;

        private PlayerContext _playerContext;
        private InputAction _interactAction;

        [Inject]
        public void Construct(PlayerContext playerContext, InputActionAsset inputActions)
        {
            _playerContext = playerContext;

            var platformerMap = inputActions.FindActionMap("Platformer", throwIfNotFound: true);
            _interactAction = platformerMap.FindAction("Interact", throwIfNotFound: true);
        }

        private void Update()
        {
            if (!_interactAction.WasPressedThisFrame())
            {
                return;
            }

            var hit = Physics2D.OverlapCircle(_interactPoint.position, _interactRadius, _interactableMask);
            if (hit != null && hit.TryGetComponent<IInteractable>(out var interactable))
            {
                interactable.Interact(_playerContext);
            }
        }
    }
}
