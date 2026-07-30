using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    public sealed class ItemService : IItemService, ISaveable
    {
        private readonly Dictionary<ItemDefinition, int> _amounts = new();
        private readonly EventBus _eventBus;
        private readonly SaveAssetRegistry _registry;

        public ItemService(EventBus eventBus, SaveAssetRegistry registry)
        {
            _eventBus = eventBus;
            _registry = registry;
        }

        public IEnumerable<KeyValuePair<ItemDefinition, int>> All => _amounts;

        public int GetAmount(ItemDefinition item)
        {
            return _amounts.GetValueOrDefault(item);
        }

        public void Add(ItemDefinition item, int amount)
        {
            var newAmount = GetAmount(item) + amount;
            _amounts[item] = newAmount;
            _eventBus.Publish(new ItemChangedEvent(item, newAmount));
        }

        public bool TrySpend(ItemDefinition item, int amount)
        {
            var current = GetAmount(item);
            if (current < amount)
            {
                return false;
            }

            var newAmount = current - amount;
            _amounts[item] = newAmount;
            _eventBus.Publish(new ItemChangedEvent(item, newAmount));
            return true;
        }

        public string SaveId => "items";

        public string Save()
        {
            var data = new SaveData();
            foreach (var pair in _amounts)
            {
                data.items.Add(_registry.GetId(pair.Key));
                data.amounts.Add(pair.Value);
            }

            return JsonUtility.ToJson(data);
        }

        public void Load(string json)
        {
            _amounts.Clear();
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                return;
            }

            for (var i = 0; i < data.items.Count; i++)
            {
                var item = _registry.GetItem(data.items[i]);
                if (item != null)
                {
                    _amounts[item] = data.amounts[i];
                    _eventBus.Publish(new ItemChangedEvent(item, data.amounts[i]));
                }
            }
        }

        [Serializable]
        private sealed class SaveData
        {
            public List<string> items = new();
            public List<int> amounts = new();
        }
    }
}
