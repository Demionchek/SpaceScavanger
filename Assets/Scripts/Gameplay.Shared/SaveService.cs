using System;
using System.Collections.Generic;
using System.IO;
using Game.Core;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.Shared
{
    public sealed class SaveService : IStartable, IDisposable
    {
        [Serializable]
        private sealed class Blob
        {
            public List<Entry> entries = new();
        }

        [Serializable]
        private sealed class Entry
        {
            public string id;
            public string json;
        }

        private readonly EventBus _eventBus;
        private readonly List<ISaveable> _saveables;

        public SaveService(EventBus eventBus, IEnumerable<ISaveable> saveables)
        {
            _eventBus = eventBus;
            _saveables = new List<ISaveable>(saveables);
        }

        private static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

        public void Start()
        {
            _eventBus.Subscribe<SaveGameRequestedEvent>(OnSaveRequested);
            _eventBus.Subscribe<LoadGameRequestedEvent>(OnLoadRequested);

            var ids = new List<string>();
            foreach (var saveable in _saveables)
            {
                ids.Add(saveable.SaveId);
            }

            Debug.Log($"[Save] Registered saveables ({_saveables.Count}): {string.Join(", ", ids)}");
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<SaveGameRequestedEvent>(OnSaveRequested);
            _eventBus.Unsubscribe<LoadGameRequestedEvent>(OnLoadRequested);
        }

        private void OnSaveRequested(SaveGameRequestedEvent _)
        {
            var blob = new Blob();
            foreach (var saveable in _saveables)
            {
                var json = saveable.Save();
                blob.entries.Add(new Entry { id = saveable.SaveId, json = json });
                Debug.Log($"[Save] {saveable.SaveId} = {json}");
            }

            File.WriteAllText(FilePath, JsonUtility.ToJson(blob));
            Debug.Log($"[Save] Wrote {blob.entries.Count} entries to {FilePath}");
        }

        private void OnLoadRequested(LoadGameRequestedEvent _)
        {
            if (!File.Exists(FilePath))
            {
                Debug.LogWarning($"[Save] No save file at {FilePath}");
                return;
            }

            var blob = JsonUtility.FromJson<Blob>(File.ReadAllText(FilePath));
            if (blob?.entries == null)
            {
                Debug.LogWarning("[Save] Save file empty or unreadable");
                return;
            }

            Debug.Log($"[Save] Loading {blob.entries.Count} entries from {FilePath}");
            _eventBus.Publish(new GameLoadStartedEvent());
            foreach (var entry in blob.entries)
            {
                var target = Find(entry.id);
                if (target == null)
                {
                    Debug.LogWarning($"[Save] No saveable registered for id '{entry.id}' — skipped");
                    continue;
                }

                Debug.Log($"[Save] Restoring {entry.id} = {entry.json}");
                target.Load(entry.json);
            }

            _eventBus.Publish(new GameLoadFinishedEvent());
            Debug.Log("[Save] Load complete");
        }

        private ISaveable Find(string id)
        {
            foreach (var saveable in _saveables)
            {
                if (saveable.SaveId == id)
                {
                    return saveable;
                }
            }

            return null;
        }
    }
}
