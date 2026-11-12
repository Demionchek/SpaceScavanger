using System;

namespace Game.Core
{
    // Мини-игры на находках в космосе. Окно не знает, что именно вскрывают:
    // сложность и ответ приходят параметрами, как в ConfirmRequestedEvent.
    public readonly struct TimingPuzzleRequestedEvent
    {
        public readonly string Title;
        public readonly int Rounds;
        public readonly float BandWidth;
        public readonly float Speed;
        public readonly float Seconds;
        public readonly Action<bool> OnFinished;

        public TimingPuzzleRequestedEvent(string title, int rounds, float bandWidth, float speed,
            float seconds, Action<bool> onFinished)
        {
            Title = title;
            Rounds = rounds;
            BandWidth = bandWidth;
            Speed = speed;
            Seconds = seconds;
            OnFinished = onFinished;
        }
    }

    // Игрок согласился вскрывать находку. Публикуется эффектом AcceptFind из
    // Yarn: сам диалог ничего не знает о том, какая находка его открыла, зато
    // слушает его ровно одна — та, что этот разговор и начала.
    public readonly struct FindAcceptedEvent
    {
    }

    public readonly struct TypingPuzzleRequestedEvent
    {
        public readonly string Title;
        public readonly string[] Words;
        public readonly float Seconds;
        public readonly Action<bool> OnFinished;

        public TypingPuzzleRequestedEvent(string title, string[] words, float seconds,
            Action<bool> onFinished)
        {
            Title = title;
            Words = words;
            Seconds = seconds;
            OnFinished = onFinished;
        }
    }
}
