using Game.Data;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.Flight
{
    public sealed class ZoneSpawner : IStartable
    {
        private readonly IZoneGenerator _generator;
        private readonly ZoneConfig _config;
        private readonly ZoneSeed _seed;

        public ZoneSpawner(IZoneGenerator generator, ZoneConfig config, ZoneSeed seed)
        {
            _generator = generator;
            _config = config;
            _seed = seed;
        }

        public void Start()
        {
            var content = _generator.Generate(_config, _seed.Value);

            foreach (var resourceSpawn in content.ResourceSpawns)
            {
                Object.Instantiate(
                    _config.ResourcePrefabs[resourceSpawn.PrefabIndex],
                    resourceSpawn.Position,
                    Quaternion.identity);
            }

            foreach (var enemyPosition in content.EnemySpawnPoints)
            {
                if (_config.EnemyPrefab != null)
                {
                    Object.Instantiate(_config.EnemyPrefab, enemyPosition, Quaternion.identity);
                }
            }

            if (content.TraderSpawnPoint.HasValue && _config.TraderPrefab != null)
            {
                Object.Instantiate(_config.TraderPrefab, content.TraderSpawnPoint.Value, Quaternion.identity);
            }
        }
    }
}
