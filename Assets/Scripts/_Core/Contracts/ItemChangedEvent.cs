namespace Game.Core
{
    public readonly struct ItemChangedEvent
    {
        public readonly ItemDefinition Item;
        public readonly int NewAmount;

        public ItemChangedEvent(ItemDefinition item, int newAmount)
        {
            Item = item;
            NewAmount = newAmount;
        }
    }
}
