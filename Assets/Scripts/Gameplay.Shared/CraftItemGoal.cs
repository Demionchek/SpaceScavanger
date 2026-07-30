using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    public sealed class CraftItemGoal : IQuestGoal, ISaveableGoal
    {
        private readonly QuestDefinition _quest;
        private readonly ItemDefinition _item;
        private readonly int _targetCount;

        private EventBus _bus;
        private int _craftedCount;

        public CraftItemGoal(QuestDefinition quest, ItemDefinition item, int targetCount)
        {
            _quest = quest;
            _item = item;
            _targetCount = targetCount;
        }

        public bool IsComplete => _craftedCount >= _targetCount;

        public float Progress => Mathf.Clamp01((float)_craftedCount / _targetCount);

        public void Bind(EventBus bus)
        {
            _bus = bus;
            bus.Subscribe<ItemCraftedEvent>(OnItemCrafted);
        }

        public void Unbind(EventBus bus)
        {
            bus.Unsubscribe<ItemCraftedEvent>(OnItemCrafted);
            _bus = null;
        }

        private void OnItemCrafted(ItemCraftedEvent evt)
        {
            if (IsComplete || evt.Item != _item)
            {
                return;
            }

            _craftedCount++;

            if (IsComplete)
            {
                _bus.Publish(new QuestCompletedEvent(_quest));
            }
        }

        public string SaveProgress() => _craftedCount.ToString();

        public void LoadProgress(string data) => int.TryParse(data, out _craftedCount);
    }
}
