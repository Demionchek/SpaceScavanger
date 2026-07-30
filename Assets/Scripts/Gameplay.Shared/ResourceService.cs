using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    public sealed class ResourceService : IResourceService, ISaveable
    {
        private readonly Dictionary<ResourceType, int> _amounts = new();
        private readonly EventBus _eventBus;

        public ResourceService(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public int GetAmount(ResourceType type)
        {
            return _amounts.GetValueOrDefault(type);
        }

        public void Add(ResourceType type, int amount)
        {
            var newAmount = GetAmount(type) + amount;
            _amounts[type] = newAmount;
            _eventBus.Publish(new ResourceChangedEvent(type, newAmount));
        }

        public bool TrySpend(ResourceType type, int amount)
        {
            var current = GetAmount(type);
            if (current < amount)
            {
                return false;
            }

            var newAmount = current - amount;
            _amounts[type] = newAmount;
            _eventBus.Publish(new ResourceChangedEvent(type, newAmount));
            return true;
        }

        public string SaveId => "resources";

        public string Save()
        {
            var data = new SaveData();
            foreach (var pair in _amounts)
            {
                data.types.Add(pair.Key.ToString());
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

            for (var i = 0; i < data.types.Count; i++)
            {
                if (Enum.TryParse<ResourceType>(data.types[i], out var type))
                {
                    _amounts[type] = data.amounts[i];
                    _eventBus.Publish(new ResourceChangedEvent(type, data.amounts[i]));
                }
            }
        }

        [Serializable]
        private sealed class SaveData
        {
            public List<string> types = new();
            public List<int> amounts = new();
        }
    }
}
