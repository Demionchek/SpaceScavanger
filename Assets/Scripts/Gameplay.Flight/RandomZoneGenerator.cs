using System.Collections.Generic;
using Game.Data;
using UnityEngine;

namespace Game.Gameplay.Flight
{
    public sealed class RandomZoneGenerator : IZoneGenerator
    {
        // Тир подменяет врагов и количество добра: чем дальше сектор, тем он злее.
        public ZoneContent Generate(ZoneConfig config, ZoneTier tier, int seed)
        {
            var random = new System.Random(seed);
            var placed = new List<Placement>();
            var enemyPrefabs = tier != null ? tier.EnemiesOr(config) : config.EnemyPrefabs;
            var enemyCount = tier != null ? tier.EnemyCount : config.EnemyCount;
            var enemyChance = tier != null ? tier.EnemySpawnChance : config.EnemySpawnChance;
            var resourceCount = tier != null ? tier.ResourceCount : config.ResourceCount;

            // Островки идут раньше всех: они крупные, и место под них должно
            // находиться, пока зона пустая.
            var islandSpawns = new List<IslandSpawnPoint>();
            for (var i = 0; i < config.IslandCount; i++)
            {
                if (random.NextDouble() > config.IslandSpawnChance ||
                    config.IslandTileSets == null || config.IslandTileSets.Length == 0)
                {
                    continue;
                }

                if (TryPlaceIsland(random, config, placed, out var island))
                {
                    islandSpawns.Add(island);
                }
            }

            // Одиночные объекты ставятся следом: их нельзя пропустить, и вокруг них
            // нужен самый большой свободный круг.
            Vector2? wormholeSpawn = null;
            if (config.WormholePrefab != null &&
                TryPlace(random, config, placed, config.LandmarkClearance, true, out var wormholePoint))
            {
                wormholeSpawn = wormholePoint;
            }

            PrefabSpawnPoint? traderSpawn = null;
            if (random.NextDouble() <= config.TraderSpawnChance && !HasNoPrefabs(config.TraderPrefabs))
            {
                traderSpawn = RandomSpawnPoint(random, config, placed, config.LandmarkClearance, true,
                    config.TraderPrefabs.Length);
            }

            PrefabSpawnPoint? questGiverSpawn = null;
            if (random.NextDouble() <= config.QuestGiverSpawnChance && !HasNoPrefabs(config.QuestGiverPrefabs))
            {
                questGiverSpawn = RandomSpawnPoint(random, config, placed, config.LandmarkClearance, true,
                    config.QuestGiverPrefabs.Length);
            }

            var enemySpawns = new List<PrefabSpawnPoint>();
            for (var i = 0; i < enemyCount; i++)
            {
                if (random.NextDouble() > enemyChance || HasNoPrefabs(enemyPrefabs))
                {
                    continue;
                }

                var leader = RandomSpawnPoint(random, config, placed, config.EnemyClearance, false,
                    enemyPrefabs.Length);
                if (!leader.HasValue)
                {
                    continue;
                }

                enemySpawns.Add(leader.Value);
                AddGroupMembers(random, config, tier, placed, leader.Value.Position, enemyPrefabs.Length,
                    enemySpawns);
            }

            var resourceSpawns = new List<PrefabSpawnPoint>();
            for (var i = 0; i < resourceCount; i++)
            {
                if (random.NextDouble() > config.ResourceSpawnChance || HasNoPrefabs(config.ResourcePrefabs))
                {
                    continue;
                }

                var spawn = RandomSpawnPoint(random, config, placed, config.ResourceClearance, false,
                    config.ResourcePrefabs.Length);
                if (spawn.HasValue)
                {
                    resourceSpawns.Add(spawn.Value);
                }
            }

            return new ZoneContent(resourceSpawns, enemySpawns, traderSpawn, questGiverSpawn,
                wormholeSpawn, islandSpawns);
        }

        // Свободное место под островок зависит от его собственного размера, а тот —
        // от точки, поэтому радиус считается для каждого кандидата заново.
        private static bool TryPlaceIsland(System.Random random, ZoneConfig config,
            List<Placement> placed, out IslandSpawnPoint island)
        {
            var attempts = Mathf.Max(1, config.PlacementAttempts);
            for (var i = 0; i < attempts; i++)
            {
                var candidate = RandomPointInArea(random, config.AreaSize);
                var clearance = AsteroidIsland.WorldRadiusFor(candidate, config.IslandRadiusTiles)
                                + Mathf.Max(0f, config.IslandMargin);

                if (Gap(candidate, clearance, config, placed) < 0f)
                {
                    continue;
                }

                placed.Add(new Placement(candidate, clearance));
                island = new IslandSpawnPoint(candidate, random.Next(config.IslandTileSets.Length));
                return true;
            }

            island = default;
            return false;
        }

        private static bool HasNoPrefabs(GameObject[] prefabs)
        {
            return prefabs == null || prefabs.Length == 0;
        }

        // Остальные в звене жмутся к вожаку: между собой у них свой тесный радиус,
        // а от чужих объектов их по-прежнему отталкивает больший из двух.
        private static void AddGroupMembers(System.Random random, ZoneConfig config, ZoneTier tier,
            List<Placement> placed, Vector2 leader, int prefabCount, List<PrefabSpawnPoint> spawns)
        {
            if (tier == null)
            {
                return;
            }

            var size = tier.RollGroupSize(random);
            for (var i = 1; i < size; i++)
            {
                if (TryPlaceNear(random, config, placed, leader, tier.GroupRadius,
                        config.GroupMemberClearance, out var point))
                {
                    spawns.Add(new PrefabSpawnPoint(point, random.Next(prefabCount)));
                }
            }
        }

        private static bool TryPlaceNear(System.Random random, ZoneConfig config, List<Placement> placed,
            Vector2 center, float radius, float clearance, out Vector2 point)
        {
            var attempts = Mathf.Max(1, config.PlacementAttempts);
            for (var i = 0; i < attempts; i++)
            {
                var candidate = ClampToArea(center + RandomInCircle(random, radius), config.AreaSize);
                if (Gap(candidate, clearance, config, placed) >= 0f)
                {
                    placed.Add(new Placement(candidate, clearance));
                    point = candidate;
                    return true;
                }
            }

            point = Vector2.zero;
            return false;
        }

        private static Vector2 RandomInCircle(System.Random random, float radius)
        {
            var angle = (float)random.NextDouble() * Mathf.PI * 2f;
            var distance = Mathf.Sqrt((float)random.NextDouble()) * radius;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        }

        private static Vector2 ClampToArea(Vector2 point, Vector2 areaSize)
        {
            return new Vector2(
                Mathf.Clamp(point.x, -areaSize.x * 0.5f, areaSize.x * 0.5f),
                Mathf.Clamp(point.y, -areaSize.y * 0.5f, areaSize.y * 0.5f));
        }

        private static PrefabSpawnPoint? RandomSpawnPoint(System.Random random, ZoneConfig config,
            List<Placement> placed, float clearance, bool required, int prefabCount)
        {
            if (!TryPlace(random, config, placed, clearance, required, out var point))
            {
                return null;
            }

            return new PrefabSpawnPoint(point, random.Next(prefabCount));
        }

        // Точка берётся случайно и проверяется на занятость; не вышло за отведённые
        // попытки — обязательный объект встаёт в самое свободное из проверенных мест,
        // остальные просто не спавнятся.
        private static bool TryPlace(System.Random random, ZoneConfig config, List<Placement> placed,
            float clearance, bool required, out Vector2 point)
        {
            var attempts = Mathf.Max(1, config.PlacementAttempts);
            var best = Vector2.zero;
            var bestGap = float.NegativeInfinity;

            for (var i = 0; i < attempts; i++)
            {
                var candidate = RandomPointInArea(random, config.AreaSize);
                var gap = Gap(candidate, clearance, config, placed);
                if (gap >= 0f)
                {
                    placed.Add(new Placement(candidate, clearance));
                    point = candidate;
                    return true;
                }

                if (gap > bestGap)
                {
                    bestGap = gap;
                    best = candidate;
                }
            }

            if (!required)
            {
                point = Vector2.zero;
                return false;
            }

            placed.Add(new Placement(best, clearance));
            point = best;
            return true;
        }

        // Насколько точка свободна: отрицательное значение — кто-то слишком близко.
        private static float Gap(Vector2 candidate, float clearance, ZoneConfig config, List<Placement> placed)
        {
            // Игрок после прыжка оказывается в центре зоны, поэтому там тоже пусто.
            var gap = candidate.magnitude - config.PlayerClearRadius;

            foreach (var other in placed)
            {
                var distance = Vector2.Distance(candidate, other.Position) - Mathf.Max(clearance, other.Clearance);
                if (distance < gap)
                {
                    gap = distance;
                }
            }

            return gap;
        }

        private static Vector2 RandomPointInArea(System.Random random, Vector2 areaSize)
        {
            var x = ((float)random.NextDouble() - 0.5f) * areaSize.x;
            var y = ((float)random.NextDouble() - 0.5f) * areaSize.y;
            return new Vector2(x, y);
        }

        private readonly struct Placement
        {
            public readonly Vector2 Position;
            public readonly float Clearance;

            public Placement(Vector2 position, float clearance)
            {
                Position = position;
                Clearance = clearance;
            }
        }
    }
}
