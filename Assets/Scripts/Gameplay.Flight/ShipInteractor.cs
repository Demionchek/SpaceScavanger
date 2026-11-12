using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Game.Gameplay.Flight
{
    public sealed class ShipInteractor : MonoBehaviour
    {
        [SerializeField] private float _interactRadius = 3f;
        [SerializeField] private LayerMask _interactableMask;

        [Header("Подсказка [E]")]
        [Tooltip("Шрифт игры. Пусто — TMP подставит свой, и подсказка выпадет из пиксельного вида.")]
        [SerializeField] private TMP_FontAsset _hintFont;

        [Tooltip("Размер в единицах мира: у лент сообщений он тоже 0.25.")]
        [SerializeField] private float _hintFontSize = 0.25f;

        [SerializeField] private Color _hintColor = Color.white;

        [Tooltip("Размер поля под текст, в единицах мира.")]
        [SerializeField] private Vector2 _hintSize = new(4f, 0.5f);

        [Tooltip("Насколько выше верхней кромки цели висит подсказка.")]
        [SerializeField] private float _hintMargin = 0.35f;

        [Tooltip("Слой отрисовки: выше всего содержимого космоса и ниже лент сообщений (100).")]
        [SerializeField] private int _hintSortingOrder = 40;

        [Tooltip("Как часто ищется цель под подсказку. Нажатие проверяет круг само, " +
                 "так что на точность взаимодействия это не влияет.")]
        [SerializeField] private float _hintRefreshInterval = 0.1f;

        private PlayerContext _playerContext;
        private IShipInputProvider _shipInput;
        private HudVisibility _hud;
        private IPauseService _pause;
        private InteractPromptLabel _prompt;
        private float _nextHintCheck;
        private bool _wasPaused;

        [Inject]
        public void Construct(PlayerContext playerContext, IShipInputProvider shipInput, HudVisibility hud,
            IPauseService pause, SpaceRoot spaceRoot, InputActionAsset inputActions)
        {
            _playerContext = playerContext;
            _shipInput = shipInput;
            _hud = hud;
            _pause = pause;

            // Ярлык живёт под SpaceRoot, а не под кораблём: вместе с космосом он
            // гаснет в интерьере, переживает регенерацию зоны и не вращается.
            var key = InputKeyLabels.First(inputActions.FindAction("Flight/Interact", throwIfNotFound: true));
            _prompt = new InteractPromptLabel(spaceRoot.transform, _hintFont, _hintFontSize, _hintColor,
                _hintSize, _hintMargin, _hintSortingOrder, key);
        }

        private void Update()
        {
            // Клавиша, которой игрок закончил мини-игру или закрыл модалку, не
            // должна в тот же миг ещё и посадить его на корабль: карты ввода
            // включаются обратно уже после паузы, и зажатая E доезжает до Interact
            // следующим кадром. Поэтому кадр снятия паузы пропускается целиком.
            var paused = _pause != null && _pause.IsPaused;
            var justResumed = _wasPaused && !paused;
            _wasPaused = paused;

            if (!paused && !justResumed && _shipInput != null && _shipInput.InteractPressed)
            {
                Interact();
            }

            if (_prompt == null)
            {
                return;
            }

            if (_hud != null && !_hud.IsVisible)
            {
                _prompt.Hide();
                return;
            }

            // Подсказка — косметика, её хватает несколько раз в секунду; позицию
            // держим каждый кадр, иначе она отстаёт от корабля.
            if (Time.time >= _nextHintCheck)
            {
                _nextHintCheck = Time.time + _hintRefreshInterval;
                Refocus();
            }

            _prompt.Follow();
        }

        private void Interact()
        {
            // Одна клавиша на всё: рядом что-то есть — работаем с ним, нет — идём
            // в корабль. Иначе новичку надо помнить две почти одинаковые клавиши.
            var hit = Overlap();
            if (hit != null && hit.TryGetComponent<IInteractable>(out var interactable))
            {
                interactable.Interact(_playerContext);

                // Действие могло сменить подсказку (квест-гивер: выдать → сдать).
                _nextHintCheck = 0f;
                return;
            }

            _playerContext.EventBus.Publish(new ModeSwitchRequestedEvent());
        }

        // Подсказку показывает тот же круг, что решает, с чем игрок взаимодействует,
        // поэтому она всегда относится ровно к тому объекту, который сработает.
        private void Refocus()
        {
            var hit = Overlap();
            if (hit != null && hit.TryGetComponent<IInteractable>(out var interactable))
            {
                _prompt.Focus(hit, interactable);
            }
            else
            {
                _prompt.Hide();
            }
        }

        private Collider2D Overlap() =>
            Physics2D.OverlapCircle(transform.position, _interactRadius, _interactableMask);

        private void OnDisable()
        {
            _prompt?.Hide();
        }
    }
}
