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
        private readonly LifetimeScope _rootScope;

        public ZoneSpawner(IZoneGenerator generator, ZoneConfig config, ZoneSeed seed, LifetimeScope rootScope)
        {
            _generator = generator;
            _config = config;
            _seed = seed;
            _rootScope = rootScope;
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
                SpawnEnemy(enemyPosition);
            }

            if (content.TraderSpawnPoint.HasValue && _config.TraderPrefab != null)
            {
                Object.Instantiate(_config.TraderPrefab, content.TraderSpawnPoint.Value, Quaternion.identity);
            }
        }

        private void SpawnEnemy(Vector2 position)
        {
            if (_config.EnemyPrefab == null)
            {
                return;
            }

            var prefabScope = _config.EnemyPrefab.GetComponent<LifetimeScope>();
            var enemyScope = _rootScope.CreateChildFromPrefab(prefabScope);
            enemyScope.transform.SetParent(null);
            enemyScope.transform.position = position;
        }
    }
}
