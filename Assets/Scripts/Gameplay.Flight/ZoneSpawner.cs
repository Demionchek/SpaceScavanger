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
        private readonly ZoneSeed _seed;
        private readonly LifetimeScope _rootScope;
        private readonly SpaceRoot _spaceRoot;
        private readonly EventBus _eventBus;
        private readonly bool _generateOnStart;

        private int _currentSeed;

        public ZoneSpawner(IZoneGenerator generator, ZoneConfig config, ZoneSeed seed,
            LifetimeScope rootScope, SpaceRoot spaceRoot, EventBus eventBus, bool generateOnStart)
        {
            _generateOnStart = generateOnStart;
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
            var data = new SaveData { seed = _currentSeed };
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
            var content = _generator.Generate(_config, _currentSeed);

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

            if (content.QuestGiverSpawn.HasValue)
            {
                SpawnItem(ZoneItemKind.QuestGiver, content.QuestGiverSpawn.Value.PrefabIndex, content.QuestGiverSpawn.Value.Position, 0f);
            }

            if (content.WormholeSpawn.HasValue)
            {
                SpawnItem(ZoneItemKind.Wormhole, 0, content.WormholeSpawn.Value, 0f);
            }
        }

        private void SpawnItem(ZoneItemKind kind, int prefabIndex, Vector2 position, float rotation)
        {
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

            var tag = instance.AddComponent<ZoneItemTag>();
            tag.Kind = kind;
            tag.PrefabIndex = prefabIndex;
        }

        private GameObject GetPrefab(ZoneItemKind kind, int index) => kind switch
        {
            ZoneItemKind.Resource => At(_config.ResourcePrefabs, index),
            ZoneItemKind.Enemy => At(_config.EnemyPrefabs, index),
            ZoneItemKind.Trader => At(_config.TraderPrefabs, index),
            ZoneItemKind.QuestGiver => At(_config.QuestGiverPrefabs, index),
            ZoneItemKind.Wormhole => _config.WormholePrefab,
            _ => null
        };

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
            public List<int> kinds = new();
            public List<int> prefabIndices = new();
            public List<float> xs = new();
            public List<float> ys = new();
            public List<float> rotations = new();
        }
    }
}
