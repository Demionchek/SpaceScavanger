using System;
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

namespace Game.UI
{
    public sealed class ConfirmDialogUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _message;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private TMP_Text _confirmLabel;
        [SerializeField] private TMP_Text _cancelLabel;

        private EventBus _eventBus;
        private IPauseService _pauseService;
        private Action _onConfirmed;
        private bool _open;

        [Inject]
        public void Construct(EventBus eventBus, IPauseService pauseService)
        {
            _eventBus = eventBus;
            _pauseService = pauseService;

            _eventBus.Subscribe<ConfirmRequestedEvent>(OnConfirmRequested);
        }

        private void Awake()
        {
            if (_confirmButton != null)
            {
                _confirmButton.onClick.AddListener(Confirm);
            }

            if (_cancelButton != null)
            {
                _cancelButton.onClick.AddListener(Cancel);
            }

            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<ConfirmRequestedEvent>(OnConfirmRequested);
        }

        // Модалка держит паузу, поэтому клавиатуру опрашиваем напрямую: карты
        // ввода в этот момент выключены.
        private void Update()
        {
            var keyboard = Keyboard.current;
            if (!_open || keyboard == null)
            {
                return;
            }

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame ||
                keyboard.spaceKey.wasPressedThisFrame)
            {
                Confirm();
            }
            else if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Cancel();
            }
        }

        private void OnConfirmRequested(ConfirmRequestedEvent evt)
        {
            if (_open)
            {
                return;
            }

            _open = true;
            _onConfirmed = evt.OnConfirmed;

            if (_message != null)
            {
                _message.text = evt.Message;
            }

            if (_confirmLabel != null)
            {
                _confirmLabel.text = evt.ConfirmLabel;
            }

            if (_cancelLabel != null)
            {
                _cancelLabel.text = evt.CancelLabel;
            }

            if (_panel != null)
            {
                _panel.SetActive(true);
            }

            _pauseService.RequestPause();
        }

        private void Confirm()
        {
            var action = _onConfirmed;
            Close();
            action?.Invoke();
        }

        private void Cancel()
        {
            Close();
        }

        // Пауза снимается ДО колбэка: подтверждённое действие может начать
        // переход режима, который сам ставит свою паузу.
        private void Close()
        {
            if (!_open)
            {
                return;
            }

            _open = false;
            _onConfirmed = null;

            if (_panel != null)
            {
                _panel.SetActive(false);
            }

            _pauseService.ReleasePause();
        }
    }
}
