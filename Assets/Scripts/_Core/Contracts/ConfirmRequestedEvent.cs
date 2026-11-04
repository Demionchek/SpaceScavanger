using System;

namespace Game.Core
{
    // Запрос модального подтверждения. Действие приходит колбэком, чтобы логика
    // осталась у того, кто спрашивает, а UI ничего не знал о смысле вопроса.
    public readonly struct ConfirmRequestedEvent
    {
        public ConfirmRequestedEvent(string message, Action onConfirmed, string confirmLabel = "Confirm",
            string cancelLabel = "Cancel")
        {
            Message = message;
            OnConfirmed = onConfirmed;
            ConfirmLabel = confirmLabel;
            CancelLabel = cancelLabel;
        }

        public string Message { get; }
        public Action OnConfirmed { get; }
        public string ConfirmLabel { get; }
        public string CancelLabel { get; }
    }
}
