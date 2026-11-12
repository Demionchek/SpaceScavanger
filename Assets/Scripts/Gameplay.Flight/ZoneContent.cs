using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Flight
{
    public readonly struct PrefabSpawnPoint
    {
        public readonly Vector2 Position;
        public readonly int PrefabIndex;

        // Поворот нужен только находкам на островках: они стоят на породе, а не
        // висят в пустоте. Остальное содержимое зоны его не задаёт.
        public readonly float Rotation;

        public PrefabSpawnPoint(Vector2 position, int prefabIndex, float rotation = 0f)
        {
            Position = position;
            PrefabIndex = prefabIndex;
            Rotation = rotation;
        }
    }

    public readonly struct IslandSpawnPoint
    {
        public readonly Vector2 Position;
        public readonly int TileSetIndex;

        public IslandSpawnPoint(Vector2 position, int tileSetIndex)
        {
            Position = position;
            TileSetIndex = tileSetIndex;
        }
    }

    public sealed class ZoneContent
    {
        public readonly List<PrefabSpawnPoint> ResourceSpawns;
        public readonly List<PrefabSpawnPoint> EnemySpawns;
        public readonly PrefabSpawnPoint? TraderSpawn;
        public readonly List<PrefabSpawnPoint> QuestGiverSpawns;
        public readonly Vector2? WormholeSpawn;
        public readonly List<IslandSpawnPoint> IslandSpawns;
        public readonly List<PrefabSpawnPoint> PuzzleSpawns;

        public ZoneContent(
            List<PrefabSpawnPoint> resourceSpawns,
            List<PrefabSpawnPoint> enemySpawns,
            PrefabSpawnPoint? traderSpawn,
            List<PrefabSpawnPoint> questGiverSpawns,
            Vector2? wormholeSpawn,
            List<IslandSpawnPoint> islandSpawns,
            List<PrefabSpawnPoint> puzzleSpawns)
        {
            ResourceSpawns = resourceSpawns;
            EnemySpawns = enemySpawns;
            TraderSpawn = traderSpawn;
            QuestGiverSpawns = questGiverSpawns;
            WormholeSpawn = wormholeSpawn;
            IslandSpawns = islandSpawns;
            PuzzleSpawns = puzzleSpawns;
        }
    }
}
