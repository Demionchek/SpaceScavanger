namespace Game.Core
{
    public readonly struct NotificationRequestedEvent
    {
        public readonly string Message;

        public NotificationRequestedEvent(string message)
        {
            Message = message;
        }
    }
}
