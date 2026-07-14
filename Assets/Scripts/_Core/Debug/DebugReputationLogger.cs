using System;
using UnityEngine;
using VContainer.Unity;

namespace Game.Core
{
    public sealed class DebugReputationLogger : IStartable, IDisposable
    {
        private readonly EventBus _eventBus;

        public DebugReputationLogger(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Start()
        {
            _eventBus.Subscribe<ReputationChangedEvent>(OnReputationChanged);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<ReputationChangedEvent>(OnReputationChanged);
        }

        private static void OnReputationChanged(ReputationChangedEvent evt)
        {
            Debug.Log($"Reputation [{evt.Group.DisplayName}]: {evt.NewValue}");
        }
    }
}
