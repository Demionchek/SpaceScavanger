using System;
using System.Collections.Generic;
using Game.Core;
using Game.Data;
using UnityEngine;
using VContainer.Unity;

namespace Game.Gameplay.Flight
{
    public sealed class ZoneSpawner : IStartable, IDisposable, ISaveable
    {
        private readonly IZoneGenerator _generator;
        private readonly ZoneConfig _config;
        private readonly ZoneProgressionConfig _progression;
        private readonly ISectorService _sectors;
        private readonly ZoneSeed _seed;
        private readonly LifetimeScope _rootScope;
        private readonly SpaceRoot _spaceRoot;
        private readonly EventBus _eventBus;
        private readonly bool _generateOnStart;

        private int _currentSeed;

        // Сектор, под который сгенерировано содержимое: по нему же префабы ищутся
        // при загрузке, иначе порядок ISaveable решал бы, из какого тира взят враг.
        private int _contentSector = 1;

        public ZoneSpawner(IZoneGenerator generator, ZoneConfig config, ZoneProgressionConfig progression,
            ISectorService sectors, ZoneSeed seed, LifetimeScope rootScope, SpaceRoot spaceRoot,
            EventBus eventBus, bool generateOnStart)
        {
            _generateOnStart = generateOnStart;
            _generator = generator;
            _config = config;
            _progression = progression;
            _sectors = sectors;
            _seed = seed;
            _rootScope = rootScope;
            _spaceRoot = spaceRoot;
            _eventBus = eventBus;
            _currentSeed = seed.Value;
        }

        public void Start()
        {
            _eventBus.Subscribe<ZoneRegenerateRequestedEvent>(OnRegenerateRequested);

            if (_generateOnStart)
            {
                Generate();
            }
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<ZoneRegenerateRequestedEvent>(OnRegenerateRequested);
        }

        public string SaveId => "zone";

        public string Save()
        {
            var data = new SaveData { seed = _currentSeed, sector = _contentSector };
            var content = _spaceRoot.Content;

            for (var i = 0; i < content.childCount; i++)
            {
                var child = content.GetChild(i);
                var tag = child.GetComponent<ZoneItemTag>();
                if (tag == null)
                {
                    continue;
                }

                data.kinds.Add((int)tag.Kind);
                data.prefabIndices.Add(tag.PrefabIndex);
                data.xs.Add(child.position.x);
                data.ys.Add(child.position.y);
                data.rotations.Add(child.eulerAngles.z);
            }

            return JsonUtility.ToJson(data);
        }

        public void Load(string json)
        {
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                return;
            }

            _currentSeed = data.seed;
            _contentSector = data.sector > 0 ? data.sector : 1;
            ClearContent();

            for (var i = 0; i < data.kinds.Count; i++)
            {
                SpawnItem((ZoneItemKind)data.kinds[i], data.prefabIndices[i],
                    new Vector2(data.xs[i], data.ys[i]), data.rotations[i]);
            }
        }

        private void OnRegenerateRequested(ZoneRegenerateRequestedEvent _)
        {
            ClearContent();
            _currentSeed++;
            Generate();
        }

        private void Generate()
        {
            _contentSector = _sectors.Current;
            var content = _generator.Generate(_config, Tier(), _contentSector, _currentSeed);

            foreach (var spawn in content.ResourceSpawns)
            {
                SpawnItem(ZoneItemKind.Resource, spawn.PrefabIndex, spawn.Position, 0f);
            }

            foreach (var spawn in content.EnemySpawns)
            {
                SpawnItem(ZoneItemKind.Enemy, spawn.PrefabIndex, spawn.Position, 0f);
            }

            if (content.TraderSpawn.HasValue)
            {
                SpawnItem(ZoneItemKind.Trader, content.TraderSpawn.Value.PrefabIndex, content.TraderSpawn.Value.Position, 0f);
            }

            foreach (var spawn in content.QuestGiverSpawns)
            {
                SpawnItem(ZoneItemKind.QuestGiver, spawn.PrefabIndex, spawn.Position, 0f);
            }

            foreach (var spawn in content.IslandSpawns)
            {
                SpawnItem(ZoneItemKind.Island, spawn.TileSetIndex, spawn.Position, 0f);
            }

            // Генератор целит в сторону островка, но силуэт известен только после
            // постройки, поэтому находка садится на кромку уже здесь. В сейв уходит
            // готовая точка, и загрузка ничего не пересчитывает — иначе отступ
            // накапливался бы с каждым разом.
            var islands = _spaceRoot.Content.GetComponentsInChildren<AsteroidIsland>();
            foreach (var spawn in content.PuzzleSpawns)
            {
                var position = spawn.Position;
                var rotation = spawn.Rotation;
                var island = NearestIsland(islands, position);

                if (island != null && island.TrySurfacePoint(position, _config.PuzzleIslandMargin,
                        out var surface, out var angle))
                {
                    position = surface;
                    rotation = angle;
                }

                SpawnItem(ZoneItemKind.Puzzle, spawn.PrefabIndex, position, rotation);
            }

            if (content.WormholeSpawn.HasValue)
            {
                SpawnItem(ZoneItemKind.Wormhole, 0, content.WormholeSpawn.Value, 0f);
            }
        }

        private void SpawnItem(ZoneItemKind kind, int prefabIndex, Vector2 position, float rotation)
        {
            // Островок собирается кодом, префаба у него нет.
            if (kind == ZoneItemKind.Island)
            {
                SpawnIsland(prefabIndex, position);
                return;
            }

            var prefab = GetPrefab(kind, prefabIndex);
            if (prefab == null)
            {
                return;
            }

            GameObject instance;
            if (IsScoped(kind))
            {
                var prefabScope = prefab.GetComponent<LifetimeScope>();
                instance = _rootScope.CreateChildFromPrefab(prefabScope).gameObject;
                instance.transform.SetParent(_spaceRoot.Content);
            }
            else
            {
                instance = _rootScope.Container.Instantiate(prefab, position, Quaternion.identity);
                instance.transform.SetParent(_spaceRoot.Content, worldPositionStays: true);
            }

            instance.transform.position = position;
            instance.transform.rotation = Quaternion.Euler(0f, 0f, rotation);

            if (kind == ZoneItemKind.Wormhole)
            {
                var tier = Tier();
                if (tier != null && tier.JumpCost > 0 &&
                    instance.TryGetComponent<WormholeComponent>(out var wormhole))
                {
                    wormhole.SetCost(tier.JumpCost);
                }
            }

            var tag = instance.AddComponent<ZoneItemTag>();
            tag.Kind = kind;
            tag.PrefabIndex = prefabIndex;
        }

        // Форма и размер выводятся из позиции, поэтому в сейве хватает точки и
        // номера набора тайлов — они уже лежат в ZoneItemTag.
        private void SpawnIsland(int tileSetIndex, Vector2 position)
        {
            var sets = _config.IslandTileSets;
            if (sets == null || tileSetIndex < 0 || tileSetIndex >= sets.Length || sets[tileSetIndex] == null)
            {
                return;
            }

            var instance = new GameObject("Island") { layer = _config.IslandLayer };
            instance.transform.SetParent(_spaceRoot.Content, worldPositionStays: true);
            instance.transform.position = position;

            instance.AddComponent<AsteroidIsland>().Build(
                sets[tileSetIndex],
                AsteroidIsland.RadiusFor(position, _config.IslandRadiusTiles),
                AsteroidIsland.SeedFor(position),
                _config.IslandLayer,
                _config.IslandSortingOrder,
                _config.IslandDetailOrder,
                _config.IslandDetailChance);

            var tag = instance.AddComponent<ZoneItemTag>();
            tag.Kind = ZoneItemKind.Island;
            tag.PrefabIndex = tileSetIndex;
        }

        private static AsteroidIsland NearestIsland(AsteroidIsland[] islands, Vector2 point)
        {
            AsteroidIsland best = null;
            var bestSqr = float.PositiveInfinity;

            foreach (var island in islands)
            {
                var sqr = ((Vector2)island.transform.position - point).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = island;
                }
            }

            return best;
        }

        private ZoneTier Tier() => _progression != null ? _progression.GetTier(_contentSector) : null;

        private GameObject[] EnemyPrefabs()
        {
            var tier = Tier();
            return tier != null ? tier.EnemiesOr(_config) : _config.EnemyPrefabs;
        }

        private GameObject GetPrefab(ZoneItemKind kind, int index) => kind switch
        {
            ZoneItemKind.Resource => ResourcePrefab(index),
            ZoneItemKind.Enemy => At(EnemyPrefabs(), index),
            ZoneItemKind.Trader => At(_config.TraderPrefabs, index),
            ZoneItemKind.QuestGiver => At(_config.QuestGiverPrefabs, index),
            ZoneItemKind.Wormhole => _config.WormholePrefab,
            ZoneItemKind.Puzzle => At(_config.PuzzlePrefabs, index),
            _ => null
        };

        private GameObject ResourcePrefab(int index)
        {
            var spawns = _config.ResourceSpawns;
            return spawns != null && index >= 0 && index < spawns.Length && spawns[index] != null
                ? spawns[index].Prefab
                : null;
        }

        private static GameObject At(GameObject[] array, int index) =>
            array != null && index >= 0 && index < array.Length ? array[index] : null;

        private static bool IsScoped(ZoneItemKind kind) =>
            kind is ZoneItemKind.Enemy or ZoneItemKind.Trader or ZoneItemKind.QuestGiver;

        private void ClearContent()
        {
            var parent = _spaceRoot.Content;

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

        [Serializable]
        private sealed class SaveData
        {
            public int seed;
            public int sector;
            public List<int> kinds = new();
            public List<int> prefabIndices = new();
            public List<float> xs = new();
            public List<float> ys = new();
            public List<float> rotations = new();
        }
    }
}
