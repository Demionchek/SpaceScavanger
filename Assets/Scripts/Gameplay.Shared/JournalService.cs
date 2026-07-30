using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    public sealed class JournalService : IJournalService, ISaveable
    {
        private readonly EventBus _eventBus;
        private readonly List<JournalEntry> _entries = new();

        public JournalService(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public IReadOnlyList<JournalEntry> Entries => _entries;

        public void Add(JournalCategory category, string message)
        {
            _entries.Add(new JournalEntry(category, message));
            _eventBus.Publish(new JournalChangedEvent());
        }

        public string SaveId => "journal";

        public string Save()
        {
            var data = new SaveData();
            foreach (var entry in _entries)
            {
                data.categories.Add(entry.Category.ToString());
                data.messages.Add(entry.Message);
            }

            return JsonUtility.ToJson(data);
        }

        public void Load(string json)
        {
            _entries.Clear();
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data != null)
            {
                for (var i = 0; i < data.messages.Count; i++)
                {
                    Enum.TryParse<JournalCategory>(data.categories[i], out var category);
                    _entries.Add(new JournalEntry(category, data.messages[i]));
                }
            }

            _eventBus.Publish(new JournalChangedEvent());
        }

        [Serializable]
        private sealed class SaveData
        {
            public List<string> categories = new();
            public List<string> messages = new();
        }
    }
}
