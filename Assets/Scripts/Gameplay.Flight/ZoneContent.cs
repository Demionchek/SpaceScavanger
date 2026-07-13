using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Flight
{
    public readonly struct ResourceSpawnPoint
    {
        public readonly Vector2 Position;
        public readonly int PrefabIndex;

        public ResourceSpawnPoint(Vector2 position, int prefabIndex)
        {
            Position = position;
            PrefabIndex = prefabIndex;
        }
    }

    public sealed class ZoneContent
    {
        public readonly List<ResourceSpawnPoint> ResourceSpawns;
        public readonly List<Vector2> EnemySpawnPoints;
        public readonly Vector2? TraderSpawnPoint;
        public readonly Vector2? QuestGiverSpawnPoint;

        public ZoneContent(
            List<ResourceSpawnPoint> resourceSpawns,
            List<Vector2> enemySpawnPoints,
            Vector2? traderSpawnPoint,
            Vector2? questGiverSpawnPoint)
        {
            ResourceSpawns = resourceSpawns;
            EnemySpawnPoints = enemySpawnPoints;
            TraderSpawnPoint = traderSpawnPoint;
            QuestGiverSpawnPoint = questGiverSpawnPoint;
        }
    }
}
