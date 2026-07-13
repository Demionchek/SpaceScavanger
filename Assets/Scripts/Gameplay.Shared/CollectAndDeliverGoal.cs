using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    public sealed class CollectAndDeliverGoal : IQuestGoal, IDeliverableGoal
    {
        private readonly QuestDefinition _quest;
        private readonly ResourceType _resource;
        private readonly int _amount;
        private readonly IResourceService _resources;

        private EventBus _bus;
        private bool _completionAnnounced;

        public CollectAndDeliverGoal(QuestDefinition quest, ResourceType resource, int amount, IResourceService resources)
        {
            _quest = quest;
            _resource = resource;
            _amount = amount;
            _resources = resources;
        }

        public bool IsComplete => _resources.GetAmount(_resource) >= _amount;

        public float Progress => Mathf.Clamp01((float)_resources.GetAmount(_resource) / _amount);

        public void Bind(EventBus bus)
        {
            _bus = bus;
            bus.Subscribe<ResourceChangedEvent>(OnResourceChanged);
        }

        public void Unbind(EventBus bus)
        {
            bus.Unsubscribe<ResourceChangedEvent>(OnResourceChanged);
            _bus = null;
        }

        public bool TryDeliver()
        {
            return _resources.TrySpend(_resource, _amount);
        }

        private void OnResourceChanged(ResourceChangedEvent evt)
        {
            if (evt.Type != _resource)
            {
                return;
            }

            if (IsComplete && !_completionAnnounced)
            {
                _completionAnnounced = true;
                _bus.Publish(new QuestCompletedEvent(_quest));
            }
            else if (!IsComplete)
            {
                _completionAnnounced = false;
            }
        }
    }
}
