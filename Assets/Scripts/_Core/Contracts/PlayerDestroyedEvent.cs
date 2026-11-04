namespace Game.Core
{
    // Корабль игрока уничтожен: сначала взрыв, следом экран поражения.
    public readonly struct PlayerDestroyedEvent
    {
        public readonly string Reason;

        public PlayerDestroyedEvent(string reason)
        {
            Reason = reason;
        }
    }
}
