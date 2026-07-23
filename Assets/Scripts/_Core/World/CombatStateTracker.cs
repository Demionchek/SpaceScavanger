using System;
using VContainer.Unity;

namespace Game.Core
{
    // Считает вступивших в бой врагов и держит ModeLock, пока их count > 0 —
    // не даёт уйти в интерьер во время боя.
    public sealed class CombatStateTracker : IStartable, IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly ModeLock _modeLock;

        private int _engagedCount;

        public CombatStateTracker(EventBus eventBus, ModeLock modeLock)
        {
            _eventBus = eventBus;
            _modeLock = modeLock;
        }

        public void Start()
        {
            _eventBus.Subscribe<CombatEngagementEvent>(OnEngagement);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<CombatEngagementEvent>(OnEngagement);
        }

        private void OnEngagement(CombatEngagementEvent evt)
        {
            if (evt.Engaged)
            {
                _engagedCount++;
                if (_engagedCount == 1)
                {
                    _modeLock.AddLock();
                }
            }
            else if (_engagedCount > 0)
            {
                _engagedCount--;
                if (_engagedCount == 0)
                {
                    _modeLock.RemoveLock();
                }
            }
        }
    }
}
