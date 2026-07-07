using Game.Core;

namespace Game.Gameplay.Shared
{
    public sealed class TradeService : ITradeService
    {
        private readonly IResourceService _resources;
        private readonly IItemService _items;

        public TradeService(IResourceService resources, IItemService items)
        {
            _resources = resources;
            _items = items;
        }

        public bool BuyResource(ResourceType resource, int unitPrice, int quantity)
        {
            if (!_resources.TrySpend(ResourceType.Credits, unitPrice * quantity))
            {
                return false;
            }

            _resources.Add(resource, quantity);
            return true;
        }

        public bool SellResource(ResourceType resource, int unitPrice, int quantity)
        {
            if (!_resources.TrySpend(resource, quantity))
            {
                return false;
            }

            _resources.Add(ResourceType.Credits, unitPrice * quantity);
            return true;
        }

        public bool BuyItem(ItemDefinition item, int unitPrice, int quantity)
        {
            if (!_resources.TrySpend(ResourceType.Credits, unitPrice * quantity))
            {
                return false;
            }

            _items.Add(item, quantity);
            return true;
        }

        public bool SellItem(ItemDefinition item, int unitPrice, int quantity)
        {
            if (!_items.TrySpend(item, quantity))
            {
                return false;
            }

            _resources.Add(ResourceType.Credits, unitPrice * quantity);
            return true;
        }
    }
}
