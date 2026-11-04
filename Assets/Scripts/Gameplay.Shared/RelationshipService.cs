using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    public sealed class RelationshipService : IRelationshipService, ISaveable
    {
        private readonly EventBus _eventBus;
        private readonly SaveAssetRegistry _registry;
        private readonly Dictionary<CharacterDefinition, int> _values = new();

        public RelationshipService(EventBus eventBus, SaveAssetRegistry registry)
        {
            _eventBus = eventBus;
            _registry = registry;
        }

        public IEnumerable<KeyValuePair<CharacterDefinition, int>> All
        {
            get
            {
                var characters = _registry.Characters;
                if (characters == null)
                {
                    yield break;
                }

                foreach (var character in characters)
                {
                    if (character != null)
                    {
                        yield return new KeyValuePair<CharacterDefinition, int>(character, Get(character));
                    }
                }
            }
        }

        public int Get(CharacterDefinition character)
        {
            return character != null && _values.TryGetValue(character, out var value) ? value : 0;
        }

        public void Add(CharacterDefinition character, int amount)
        {
            if (character == null || amount == 0)
            {
                return;
            }

            var newValue = Get(character) + amount;
            _values[character] = newValue;
            _eventBus.Publish(new RelationshipChangedEvent(character, newValue));
        }

        public string SaveId => "relationships";

        public string Save()
        {
            var data = new SaveData();
            foreach (var pair in _values)
            {
                data.characters.Add(_registry.GetId(pair.Key));
                data.values.Add(pair.Value);
            }

            return JsonUtility.ToJson(data);
        }

        public void Load(string json)
        {
            _values.Clear();
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                return;
            }

            for (var i = 0; i < data.characters.Count; i++)
            {
                var character = _registry.GetCharacter(data.characters[i]);
                if (character != null)
                {
                    _values[character] = data.values[i];
                    _eventBus.Publish(new RelationshipChangedEvent(character, data.values[i]));
                }
            }
        }

        [Serializable]
        private sealed class SaveData
        {
            public List<string> characters = new();
            public List<int> values = new();
        }
    }
}
