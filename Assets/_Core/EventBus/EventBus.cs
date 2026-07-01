using System;
using System.Collections.Generic;

namespace Game.Core
{
    public sealed class EventBus
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

        public void Subscribe<TEvent>(Action<TEvent> handler)
        {
            var eventType = typeof(TEvent);
            if (!_subscribers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Delegate>();
                _subscribers.Add(eventType, handlers);
            }

            handlers.Add(handler);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            if (_subscribers.TryGetValue(typeof(TEvent), out var handlers))
            {
                handlers.Remove(handler);
            }
        }

        public void Publish<TEvent>(TEvent eventData)
        {
            if (!_subscribers.TryGetValue(typeof(TEvent), out var handlers) || handlers.Count == 0)
            {
                return;
            }

            // Snapshot: handlers may subscribe/unsubscribe in reaction to this event.
            var snapshot = handlers.ToArray();
            foreach (var handler in snapshot)
            {
                ((Action<TEvent>)handler).Invoke(eventData);
            }
        }
    }
}
