using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    public sealed class ReputationService : IReputationService, ISaveable
    {
        private readonly EventBus _eventBus;
        private readonly SaveAssetRegistry _registry;
        private readonly Dictionary<NpcGroup, int> _reputation = new();

        public ReputationService(EventBus eventBus, SaveAssetRegistry registry)
        {
            _eventBus = eventBus;
            _registry = registry;
        }

        public IEnumerable<KeyValuePair<NpcGroup, int>> All
        {
            get
            {
                var groups = _registry.NpcGroups;
                if (groups == null)
                {
                    yield break;
                }

                foreach (var group in groups)
                {
                    if (group != null)
                    {
                        yield return new KeyValuePair<NpcGroup, int>(group, GetReputation(group));
                    }
                }
            }
        }

        public int GetReputation(NpcGroup group)
        {
            return _reputation.TryGetValue(group, out var value) ? value : 0;
        }

        public void Add(NpcGroup group, int amount)
        {
            if (group == null || amount == 0)
            {
                return;
            }

            var newValue = GetReputation(group) + amount;
            _reputation[group] = newValue;
            _eventBus.Publish(new ReputationChangedEvent(group, newValue));
        }

        public string SaveId => "reputation";

        public string Save()
        {
            var data = new SaveData();
            foreach (var pair in _reputation)
            {
                data.groups.Add(_registry.GetId(pair.Key));
                data.values.Add(pair.Value);
            }

            return JsonUtility.ToJson(data);
        }

        public void Load(string json)
        {
            _reputation.Clear();
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                return;
            }

            for (var i = 0; i < data.groups.Count; i++)
            {
                var group = _registry.GetNpcGroup(data.groups[i]);
                if (group != null)
                {
                    _reputation[group] = data.values[i];
                    _eventBus.Publish(new ReputationChangedEvent(group, data.values[i]));
                }
            }
        }

        [Serializable]
        private sealed class SaveData
        {
            public List<string> groups = new();
            public List<int> values = new();
        }
    }
}
