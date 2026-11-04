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

        [Tooltip("Как часто ищется объект под подсказку. Нажатие проверяет круг само, " +
                 "так что на точность взаимодействия это не влияет.")]
        [SerializeField] private float _hintRefreshInterval = 0.1f;

        private PlayerContext _playerContext;
        private InputAction _interactAction;
        private InteractionHint _focused;
        private float _nextHintCheck;

        [Inject]
        public void Construct(PlayerContext playerContext, InputActionAsset inputActions)
        {
            _playerContext = playerContext;

            var platformerMap = inputActions.FindActionMap("Platformer", throwIfNotFound: true);
            _interactAction = platformerMap.FindAction("Interact", throwIfNotFound: true);
        }

        private void Update()
        {
            if (_interactAction.WasPressedThisFrame())
            {
                Interact();
            }

            // Подсказка — косметика, её хватает несколько раз в секунду.
            if (Time.time < _nextHintCheck)
            {
                return;
            }

            _nextHintCheck = Time.time + _hintRefreshInterval;
            var hit = Overlap();
            Focus(hit != null ? hit.GetComponent<InteractionHint>() : null);
        }

        private void Interact()
        {
            var hit = Overlap();
            if (hit == null || !hit.TryGetComponent<IInteractable>(out var interactable))
            {
                // Одна клавиша на всё: рядом ничего нет — значит игрок хочет в полёт.
                _playerContext.EventBus.Publish(new ModeSwitchRequestedEvent());
                return;
            }

            interactable.Interact(_playerContext);

            if (hit.TryGetComponent<InteractionHint>(out var hint))
            {
                hint.MarkUsed();
            }

            // Действие могло сменить подсказку (интерком: звонок → сдача квоты).
            _nextHintCheck = 0f;
        }

        private Collider2D Overlap() =>
            Physics2D.OverlapCircle(_interactPoint.position, _interactRadius, _interactableMask);

        private void OnDisable()
        {
            Focus(null);
        }

        // Подсказку показывает тот же круг, что решает, с чем игрок взаимодействует,
        // поэтому она всегда относится ровно к тому объекту, который сработает.
        private void Focus(InteractionHint hint)
        {
            if (_focused == hint)
            {
                return;
            }

            if (_focused != null)
            {
                _focused.SetFocused(false);
            }

            _focused = hint;

            if (_focused != null)
            {
                _focused.SetFocused(true);
            }
        }
    }
}
