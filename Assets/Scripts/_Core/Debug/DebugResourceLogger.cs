using System;
using UnityEngine;
using VContainer.Unity;

namespace Game.Core
{
    public sealed class DebugResourceLogger : IStartable, IDisposable
    {
        private readonly EventBus _eventBus;

        public DebugResourceLogger(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public void Start()
        {
            _eventBus.Subscribe<ResourceChangedEvent>(OnResourceChanged);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<ResourceChangedEvent>(OnResourceChanged);
        }

        private static void OnResourceChanged(ResourceChangedEvent evt)
        {
            Debug.Log($"Resource {evt.Type}: {evt.NewAmount}");
        }
    }
}
