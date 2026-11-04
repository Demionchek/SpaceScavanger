using System;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public sealed class SpeakerIconEntry
    {
        [Tooltip("Имя говорящего из Yarn-строки (\"Officer: ...\").")]
        public string Speaker;

        public Sprite Icon;
    }

    [CreateAssetMenu(menuName = "Game/UI/Speaker Icon Set", fileName = "SpeakerIconSet")]
    public sealed class SpeakerIconSet : ScriptableObject
    {
        [SerializeField] private SpeakerIconEntry[] _entries;

        // Иконка есть не у каждого говорящего — null здесь нормальный результат.
        public Sprite GetIcon(string speaker)
        {
            if (_entries == null || string.IsNullOrWhiteSpace(speaker))
            {
                return null;
            }

            foreach (var entry in _entries)
            {
                if (entry != null && string.Equals(entry.Speaker, speaker, StringComparison.OrdinalIgnoreCase))
                {
                    return entry.Icon;
                }
            }

            return null;
        }
    }
}
