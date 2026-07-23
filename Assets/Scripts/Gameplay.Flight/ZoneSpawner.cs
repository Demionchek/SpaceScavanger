using System;
using Game.Core;
using Game.Data;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.Flight
{
    public sealed class ZoneSpawner : IStartable, IDisposable
    {
        private readonly IZoneGenerator _generator;
        private readonly ZoneConfig _config;
        private readonly ZoneSeed _seed;
        private readonly LifetimeScope _rootScope;
        private readonly SpaceRoot _spaceRoot;
        private readonly EventBus _eventBus;

        private int _currentSeed;

        public ZoneSpawner(IZoneGenerator generator, ZoneConfig config, ZoneSeed seed,
            LifetimeScope rootScope, SpaceRoot spaceRoot, EventBus eventBus)
        {
            _generator = generator;
            _config = config;
            _seed = seed;
            _rootScope = rootScope;
            _spaceRoot = spaceRoot;
            _eventBus = eventBus;
            _currentSeed = seed.Value;
        }

        public void Start()
        {
            _eventBus.Subscribe<ZoneRegenerateRequestedEvent>(OnRegenerateRequested);
            Generate();
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<ZoneRegenerateRequestedEvent>(OnRegenerateRequested);
        }

        private void OnRegenerateRequested(ZoneRegenerateRequestedEvent _)
        {
            ClearContent();
            // Новый seed — иначе перегенерация даст ту же самую зону.
            _currentSeed++;
            Generate();
        }

        private void Generate()
        {
            var content = _generator.Generate(_config, _currentSeed);
            var parent = _spaceRoot.Content;

            foreach (var resourceSpawn in content.ResourceSpawns)
            {
                var instance = _rootScope.Container.Instantiate(
                    _config.ResourcePrefabs[resourceSpawn.PrefabIndex],
                    resourceSpawn.Position,
                    Quaternion.identity);
                instance.transform.SetParent(parent, worldPositionStays: true);
            }

            foreach (var enemySpawn in content.EnemySpawns)
            {
                SpawnScopedPrefab(_config.EnemyPrefabs[enemySpawn.PrefabIndex], enemySpawn.Position, parent);
            }

            if (content.TraderSpawn.HasValue)
            {
                SpawnScopedPrefab(_config.TraderPrefabs[content.TraderSpawn.Value.PrefabIndex], content.TraderSpawn.Value.Position, parent);
            }

            if (content.QuestGiverSpawn.HasValue)
            {
                SpawnScopedPrefab(_config.QuestGiverPrefabs[content.QuestGiverSpawn.Value.PrefabIndex], content.QuestGiverSpawn.Value.Position, parent);
            }
        }

        // Уничтожаем всё содержимое космоса. Игрок, vcam и фон — вне Content,
        // поэтому не затрагиваются.
        private void ClearContent()
        {
            var parent = _spaceRoot.Content;

            // Защита: если Content не назначен и упал на сам SpaceRoot, очистка
            // снесла бы игрока и камеру — не делаем этого.
            if (parent == _spaceRoot.transform)
            {
                Debug.LogWarning("SpaceRoot.Content не назначен — регенерация пропущена, " +
                    "иначе удалился бы игрок. Создай дочерний Content и назначь его в SpaceRoot.");
                return;
            }

            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
            }
        }

        private void SpawnScopedPrefab(GameObject prefab, Vector2 position, Transform parent)
        {
            if (prefab == null)
            {
                return;
            }

            var prefabScope = prefab.GetComponent<LifetimeScope>();
            var instanceScope = _rootScope.CreateChildFromPrefab(prefabScope);
            instanceScope.transform.SetParent(parent);
            instanceScope.transform.position = position;
        }
    }
}
