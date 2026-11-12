using System;
using UnityEngine;
using VContainer.Unity;

namespace Game.Core
{
    public sealed class DebugReputationLogger : IStartable, IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly DebugMode _debug;

        public DebugReputationLogger(EventBus eventBus, DebugMode debug)
        {
            _eventBus = eventBus;
            _debug = debug;
        }

        public void Start()
        {
            _eventBus.Subscribe<ReputationChangedEvent>(OnReputationChanged);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<ReputationChangedEvent>(OnReputationChanged);
        }

        private void OnReputationChanged(ReputationChangedEvent evt)
        {
            if (_debug.Enabled)
            {
                Debug.Log($"Reputation [{evt.Group.DisplayName}]: {evt.NewValue}");
            }
        }
    }
}
