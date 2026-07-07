using System.Collections.Generic;
using Game.Core;

namespace Game.Gameplay.Shared
{
    public sealed class ItemService : IItemService
    {
        private readonly Dictionary<ItemDefinition, int> _amounts = new();
        private readonly EventBus _eventBus;

        public ItemService(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public int GetAmount(ItemDefinition item)
        {
            return _amounts.GetValueOrDefault(item);
        }

        public void Add(ItemDefinition item, int amount)
        {
            var newAmount = GetAmount(item) + amount;
            _amounts[item] = newAmount;
            _eventBus.Publish(new ItemChangedEvent(item, newAmount));
        }

        public bool TrySpend(ItemDefinition item, int amount)
        {
            var current = GetAmount(item);
            if (current < amount)
            {
                return false;
            }

            var newAmount = current - amount;
            _amounts[item] = newAmount;
            _eventBus.Publish(new ItemChangedEvent(item, newAmount));
            return true;
        }
    }
}
