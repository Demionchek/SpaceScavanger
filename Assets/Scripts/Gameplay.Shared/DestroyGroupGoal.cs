using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    public sealed class DestroyGroupGoal : IQuestGoal
    {
        private readonly QuestDefinition _quest;
        private readonly string _groupTag;
        private readonly int _targetCount;

        private EventBus _bus;
        private int _destroyedCount;

        public DestroyGroupGoal(QuestDefinition quest, string groupTag, int targetCount)
        {
            _quest = quest;
            _groupTag = groupTag;
            _targetCount = targetCount;
        }

        public bool IsComplete => _destroyedCount >= _targetCount;

        public float Progress => Mathf.Clamp01((float)_destroyedCount / _targetCount);

        public void Bind(EventBus bus)
        {
            _bus = bus;
            bus.Subscribe<EnemyDestroyedEvent>(OnEnemyDestroyed);
        }

        public void Unbind(EventBus bus)
        {
            bus.Unsubscribe<EnemyDestroyedEvent>(OnEnemyDestroyed);
            _bus = null;
        }

        private void OnEnemyDestroyed(EnemyDestroyedEvent evt)
        {
            if (IsComplete || evt.GroupTag != _groupTag)
            {
                return;
            }

            _destroyedCount++;

            if (IsComplete)
            {
                _bus.Publish(new QuestCompletedEvent(_quest));
            }
        }
    }
}
