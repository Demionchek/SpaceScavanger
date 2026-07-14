namespace Game.Core
{
    public readonly struct EnemyDestroyedEvent
    {
        public readonly string GroupTag;

        public EnemyDestroyedEvent(string groupTag)
        {
            GroupTag = groupTag;
        }
    }
}
