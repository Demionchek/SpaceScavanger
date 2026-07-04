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

            var resourceSpawns = new List<ResourceSpawnPoint>();
            for (var i = 0; i < config.ResourceCount; i++)
            {
                if (random.NextDouble() > config.ResourceSpawnChance || config.ResourcePrefabs.Length == 0)
                {
                    continue;
                }

                var prefabIndex = random.Next(config.ResourcePrefabs.Length);
                resourceSpawns.Add(new ResourceSpawnPoint(RandomPointInArea(random, config.AreaSize), prefabIndex));
            }

            var enemySpawns = new List<Vector2>();
            for (var i = 0; i < config.EnemyCount; i++)
            {
                if (random.NextDouble() <= config.EnemySpawnChance)
                {
                    enemySpawns.Add(RandomPointInArea(random, config.AreaSize));
                }
            }

            Vector2? traderSpawn = random.NextDouble() <= config.TraderSpawnChance
                ? RandomPointInArea(random, config.AreaSize)
                : null;

            return new ZoneContent(resourceSpawns, enemySpawns, traderSpawn);
        }

        private static Vector2 RandomPointInArea(System.Random random, Vector2 areaSize)
        {
            var x = ((float)random.NextDouble() - 0.5f) * areaSize.x;
            var y = ((float)random.NextDouble() - 0.5f) * areaSize.y;
            return new Vector2(x, y);
        }
    }
}
