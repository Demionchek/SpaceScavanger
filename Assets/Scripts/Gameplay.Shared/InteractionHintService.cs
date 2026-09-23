using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    // Помнит, какими объектами интерьера игрок уже пользовался. Живёт в главном
    // scope: сцена ShipInterior выгружается, а подсказки не должны возвращаться.
    public sealed class InteractionHintService : IInteractionHintService, ISaveable
    {
        private readonly HashSet<string> _used = new();

        public bool IsUsed(string id) => _used.Contains(id);

        public void MarkUsed(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                _used.Add(id);
            }
        }

        public string SaveId => "interaction_hints";

        public string Save() => JsonUtility.ToJson(new SaveData { used = new List<string>(_used) });

        public void Load(string json)
        {
            var data = JsonUtility.FromJson<SaveData>(json);
            _used.Clear();
            if (data?.used != null)
            {
                _used.UnionWith(data.used);
            }
        }

        [Serializable]
        private sealed class SaveData
        {
            public List<string> used;
        }
    }
}
