using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Flight
{
    public readonly struct PrefabSpawnPoint
    {
        public readonly Vector2 Position;
        public readonly int PrefabIndex;

        public PrefabSpawnPoint(Vector2 position, int prefabIndex)
        {
            Position = position;
            PrefabIndex = prefabIndex;
        }
    }

    public sealed class ZoneContent
    {
        public readonly List<PrefabSpawnPoint> ResourceSpawns;
        public readonly List<PrefabSpawnPoint> EnemySpawns;
        public readonly PrefabSpawnPoint? TraderSpawn;
        public readonly PrefabSpawnPoint? QuestGiverSpawn;

        public ZoneContent(
            List<PrefabSpawnPoint> resourceSpawns,
            List<PrefabSpawnPoint> enemySpawns,
            PrefabSpawnPoint? traderSpawn,
            PrefabSpawnPoint? questGiverSpawn)
        {
            ResourceSpawns = resourceSpawns;
            EnemySpawns = enemySpawns;
            TraderSpawn = traderSpawn;
            QuestGiverSpawn = questGiverSpawn;
        }
    }
}
