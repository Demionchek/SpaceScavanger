using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    public sealed class CollectObjectsGoal : IQuestGoal, ISaveableGoal
    {
        private readonly QuestDefinition _quest;
        private readonly int _count;

        private EventBus _bus;
        private int _collected;

        public CollectObjectsGoal(QuestDefinition quest, int count)
        {
            _quest = quest;
            _count = Mathf.Max(1, count);
        }

        public bool IsComplete => _collected >= _count;

        public float Progress => Mathf.Clamp01((float)_collected / _count);

        public void Bind(EventBus bus)
        {
            _bus = bus;
            bus.Subscribe<ResourceObjectCollectedEvent>(OnCollected);
        }

        public void Unbind(EventBus bus)
        {
            bus.Unsubscribe<ResourceObjectCollectedEvent>(OnCollected);
            _bus = null;
        }

        public string SaveProgress() => _collected.ToString();

        public void LoadProgress(string data) => int.TryParse(data, out _collected);

        private void OnCollected(ResourceObjectCollectedEvent _)
        {
            if (IsComplete)
            {
                return;
            }

            _collected++;

            if (IsComplete)
            {
                _bus.Publish(new QuestCompletedEvent(_quest));
            }
        }
    }
}
