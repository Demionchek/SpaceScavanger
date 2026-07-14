using System.Collections.Generic;
using Game.Data;
using UnityEngine;

namespace Game.Gameplay.Flight
{
    public sealed class RandomZoneGenerator : IZoneGenerator
    {
        public ZoneContent Generate(ZoneConfig config, int seed)
        {
            var random = new System.Random(seed);

            var resourceSpawns = new List<PrefabSpawnPoint>();
            for (var i = 0; i < config.ResourceCount; i++)
            {
                if (random.NextDouble() > config.ResourceSpawnChance || config.ResourcePrefabs.Length == 0)
                {
                    continue;
                }

                resourceSpawns.Add(RandomSpawnPoint(random, config.AreaSize, config.ResourcePrefabs.Length));
            }

            var enemySpawns = new List<PrefabSpawnPoint>();
            for (var i = 0; i < config.EnemyCount; i++)
            {
                if (random.NextDouble() > config.EnemySpawnChance || HasNoPrefabs(config.EnemyPrefabs))
                {
                    continue;
                }

                enemySpawns.Add(RandomSpawnPoint(random, config.AreaSize, config.EnemyPrefabs.Length));
            }

            PrefabSpawnPoint? traderSpawn = null;
            if (random.NextDouble() <= config.TraderSpawnChance && !HasNoPrefabs(config.TraderPrefabs))
            {
                traderSpawn = RandomSpawnPoint(random, config.AreaSize, config.TraderPrefabs.Length);
            }

            PrefabSpawnPoint? questGiverSpawn = null;
            if (random.NextDouble() <= config.QuestGiverSpawnChance && !HasNoPrefabs(config.QuestGiverPrefabs))
            {
                questGiverSpawn = RandomSpawnPoint(random, config.AreaSize, config.QuestGiverPrefabs.Length);
            }

            return new ZoneContent(resourceSpawns, enemySpawns, traderSpawn, questGiverSpawn);
        }

        private static bool HasNoPrefabs(GameObject[] prefabs)
        {
            return prefabs == null || prefabs.Length == 0;
        }

        private static PrefabSpawnPoint RandomSpawnPoint(System.Random random, Vector2 areaSize, int prefabCount)
        {
            return new PrefabSpawnPoint(RandomPointInArea(random, areaSize), random.Next(prefabCount));
        }

        private static Vector2 RandomPointInArea(System.Random random, Vector2 areaSize)
        {
            var x = ((float)random.NextDouble() - 0.5f) * areaSize.x;
            var y = ((float)random.NextDouble() - 0.5f) * areaSize.y;
            return new Vector2(x, y);
        }
    }
}
