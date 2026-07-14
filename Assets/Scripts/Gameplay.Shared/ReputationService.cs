using System.Collections.Generic;
using Game.Core;

namespace Game.Gameplay.Shared
{
    public sealed class ReputationService : IReputationService
    {
        private readonly EventBus _eventBus;
        private readonly Dictionary<NpcGroup, int> _reputation = new();

        public ReputationService(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public int GetReputation(NpcGroup group)
        {
            return _reputation.TryGetValue(group, out var value) ? value : 0;
        }

        public void Add(NpcGroup group, int amount)
        {
            if (group == null || amount == 0)
            {
                return;
            }

            var newValue = GetReputation(group) + amount;
            _reputation[group] = newValue;
            _eventBus.Publish(new ReputationChangedEvent(group, newValue));
        }
    }
}
