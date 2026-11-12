using System;
using UnityEngine;
using VContainer.Unity;

namespace Game.Core
{
    public sealed class DebugResourceLogger : IStartable, IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly DebugMode _debug;

        public DebugResourceLogger(EventBus eventBus, DebugMode debug)
        {
            _eventBus = eventBus;
            _debug = debug;
        }

        public void Start()
        {
            _eventBus.Subscribe<ResourceChangedEvent>(OnResourceChanged);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<ResourceChangedEvent>(OnResourceChanged);
        }

        private void OnResourceChanged(ResourceChangedEvent evt)
        {
            if (_debug.Enabled)
            {
                Debug.Log($"Resource {evt.Type}: {evt.NewAmount}");
            }
        }
    }
}
