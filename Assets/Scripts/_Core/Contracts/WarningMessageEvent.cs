namespace Game.Core
{
    // Разовое предупреждение в тот же баннер, где висит текст про границу зоны.
    public readonly struct WarningMessageEvent
    {
        public readonly string Message;

        public WarningMessageEvent(string message)
        {
            Message = message;
        }
    }
}
