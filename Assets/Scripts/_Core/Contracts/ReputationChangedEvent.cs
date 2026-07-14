namespace Game.Core
{
    public readonly struct ReputationChangedEvent
    {
        public readonly NpcGroup Group;
        public readonly int NewValue;

        public ReputationChangedEvent(NpcGroup group, int newValue)
        {
            Group = group;
            NewValue = newValue;
        }
    }
}
