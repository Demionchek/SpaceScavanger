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
                _rootScope.Container.Instantiate(
                    _config.ResourcePrefabs[resourceSpawn.PrefabIndex],
                    resourceSpawn.Position,
                    Quaternion.identity);
            }

            foreach (var enemyPosition in content.EnemySpawnPoints)
            {
                SpawnScopedPrefab(_config.EnemyPrefab, enemyPosition);
            }

            if (content.TraderSpawnPoint.HasValue)
            {
                SpawnScopedPrefab(_config.TraderPrefab, content.TraderSpawnPoint.Value);
            }

            if (content.QuestGiverSpawnPoint.HasValue)
            {
                SpawnScopedPrefab(_config.QuestGiverPrefab, content.QuestGiverSpawnPoint.Value);
            }
        }

        private void SpawnScopedPrefab(GameObject prefab, Vector2 position)
        {
            if (prefab == null)
            {
                return;
            }

            var prefabScope = prefab.GetComponent<LifetimeScope>();
            var instanceScope = _rootScope.CreateChildFromPrefab(prefabScope);
            instanceScope.transform.SetParent(null);
            instanceScope.transform.position = position;
        }
    }
}
