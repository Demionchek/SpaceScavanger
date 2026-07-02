namespace Game.Core
{
    public readonly struct ResourceChangedEvent
    {
        public readonly ResourceType Type;
        public readonly int NewAmount;

        public ResourceChangedEvent(ResourceType type, int newAmount)
        {
            Type = type;
            NewAmount = newAmount;
        }
    }
}
