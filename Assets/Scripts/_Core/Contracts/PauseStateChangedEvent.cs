namespace Game.Core
{
    public readonly struct PauseStateChangedEvent
    {
        public readonly bool IsPaused;

        public PauseStateChangedEvent(bool isPaused)
        {
            IsPaused = isPaused;
        }
    }
}
