using System;
using System.Collections.Generic;
using VContainer.Unity;

namespace Game.Core
{
    public sealed class GameNotificationsSource : IStartable, IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly Dictionary<ResourceType, int> _lastResource = new();
        private readonly Dictionary<NpcGroup, int> _lastReputation = new();

        public GameNotificationsSource(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Start()
        {
            _eventBus.Subscribe<ResourceChangedEvent>(OnResourceChanged);
            _eventBus.Subscribe<ReputationChangedEvent>(OnReputationChanged);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<ResourceChangedEvent>(OnResourceChanged);
            _eventBus.Unsubscribe<ReputationChangedEvent>(OnReputationChanged);
        }

        private void OnResourceChanged(ResourceChangedEvent evt)
        {
            var delta = evt.NewAmount - _lastResource.GetValueOrDefault(evt.Type);
            _lastResource[evt.Type] = evt.NewAmount;

            if (delta != 0)
            {
                _eventBus.Publish(new NotificationRequestedEvent($"{Signed(delta)} {evt.Type}"));
            }
        }

        private void OnReputationChanged(ReputationChangedEvent evt)
        {
            var delta = evt.NewValue - _lastReputation.GetValueOrDefault(evt.Group);
            _lastReputation[evt.Group] = evt.NewValue;

            if (delta != 0)
            {
                _eventBus.Publish(new NotificationRequestedEvent($"{evt.Group} reputation {Signed(delta)}"));
            }
        }

        private static string Signed(int value) => value > 0 ? $"+{value}" : value.ToString();
    }
}
