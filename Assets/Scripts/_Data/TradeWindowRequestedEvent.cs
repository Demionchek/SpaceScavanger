namespace Game.Data
{
    public readonly struct TradeWindowRequestedEvent
    {
        public readonly TraderInventory Inventory;

        public TradeWindowRequestedEvent(TraderInventory inventory)
        {
            Inventory = inventory;
        }
    }
}
