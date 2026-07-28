namespace Game.Core
{
    public readonly struct GameOverEvent
    {
        public readonly string Reason;

        public GameOverEvent(string reason)
        {
            Reason = reason;
        }
    }
}
