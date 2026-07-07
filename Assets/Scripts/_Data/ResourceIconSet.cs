using System;
using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public sealed class ResourceIconEntry
    {
        public ResourceType Resource;
        public Sprite Icon;
    }

    [CreateAssetMenu(menuName = "Game/UI/Resource Icon Set", fileName = "ResourceIconSet")]
    public sealed class ResourceIconSet : ScriptableObject
    {
        [SerializeField] private ResourceIconEntry[] _entries;

        public Sprite GetIcon(ResourceType type)
        {
            foreach (var entry in _entries)
            {
                if (entry.Resource == type)
                {
                    return entry.Icon;
                }
            }

            return null;
        }
    }
}
