using UnityEngine;

namespace Game.Core
{
    public readonly struct EnemyGroupSpawnRequestedEvent
    {
        public readonly GameObject Prefab;
        public readonly int Count;
        public readonly string GroupTag;
        public readonly float MinDistanceFromPlayer;
        public readonly float MaxDistanceFromPlayer;
        public readonly float MinSpacing;
        public readonly float ClearRadius;

        public EnemyGroupSpawnRequestedEvent(
            GameObject prefab,
            int count,
            string groupTag,
            float minDistanceFromPlayer,
            float maxDistanceFromPlayer,
            float minSpacing,
            float clearRadius)
        {
            Prefab = prefab;
            Count = count;
            GroupTag = groupTag;
            MinDistanceFromPlayer = minDistanceFromPlayer;
            MaxDistanceFromPlayer = maxDistanceFromPlayer;
            MinSpacing = minSpacing;
            ClearRadius = clearRadius;
        }
    }
}
