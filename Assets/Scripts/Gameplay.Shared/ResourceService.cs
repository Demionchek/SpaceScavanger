using System.Collections.Generic;
using Game.Core;

namespace Game.Gameplay.Shared
{
    public sealed class ResourceService : IResourceService
    {
        private readonly Dictionary<ResourceType, int> _amounts = new();
        private readonly EventBus _eventBus;

        public ResourceService(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public int GetAmount(ResourceType type)
        {
            return _amounts.GetValueOrDefault(type);
        }

        public void Add(ResourceType type, int amount)
        {
            var newAmount = GetAmount(type) + amount;
            _amounts[type] = newAmount;
            _eventBus.Publish(new ResourceChangedEvent(type, newAmount));
        }

        public bool TrySpend(ResourceType type, int amount)
        {
            var current = GetAmount(type);
            if (current < amount)
            {
                return false;
            }

            var newAmount = current - amount;
            _amounts[type] = newAmount;
            _eventBus.Publish(new ResourceChangedEvent(type, newAmount));
            return true;
        }
    }
}
