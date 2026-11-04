using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Flight
{
    public sealed class ShipInteractor : MonoBehaviour
    {
        [SerializeField] private float _interactRadius = 3f;
        [SerializeField] private LayerMask _interactableMask;

        private PlayerContext _playerContext;
        private IShipInputProvider _shipInput;

        [Inject]
        public void Construct(PlayerContext playerContext, IShipInputProvider shipInput)
        {
            _playerContext = playerContext;
            _shipInput = shipInput;
        }

        private void Update()
        {
            if (!_shipInput.InteractPressed)
            {
                return;
            }

            // Одна клавиша на всё: рядом что-то есть — работаем с ним, нет — идём
            // в корабль. Иначе новичку надо помнить две почти одинаковые клавиши.
            var hit = Physics2D.OverlapCircle(transform.position, _interactRadius, _interactableMask);
            if (hit != null && hit.TryGetComponent<IInteractable>(out var interactable))
            {
                interactable.Interact(_playerContext);
                return;
            }

            _playerContext.EventBus.Publish(new ModeSwitchRequestedEvent());
        }
    }
}
