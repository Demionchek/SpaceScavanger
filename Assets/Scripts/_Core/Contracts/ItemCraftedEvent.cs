namespace Game.Core
{
    public readonly struct ItemCraftedEvent
    {
        public readonly ItemDefinition Item;

        public ItemCraftedEvent(ItemDefinition item)
        {
            Item = item;
        }
    }
}
