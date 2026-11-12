using System.Collections.Generic;
using Game.Data;
using UnityEngine;

namespace Game.Gameplay.Flight
{
    public sealed class RandomZoneGenerator : IZoneGenerator
    {
        // Тир подменяет врагов и количество добра: чем дальше сектор, тем он злее.
        public ZoneContent Generate(ZoneConfig config, ZoneTier tier, int sector, int seed)
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

            // Квестодателей в секторе несколько, но двух одинаковых быть не должно:
            // префаб каждому достаётся свой. Первому место ищется обязательно —
            // сектор без заказов остался бы пустым.
            var questGiverSpawns = new List<PrefabSpawnPoint>();
            if (!HasNoPrefabs(config.QuestGiverPrefabs))
            {
                var prefabIndices = ShuffledIndices(random, config.QuestGiverPrefabs.Length);
                var giverCount = Mathf.Min(Mathf.Max(1, config.QuestGiverCount), prefabIndices.Count);

                for (var i = 0; i < giverCount; i++)
                {
                    if (random.NextDouble() > config.QuestGiverSpawnChance)
                    {
                        continue;
                    }

                    if (TryPlace(random, config, placed, config.LandmarkClearance, i == 0, out var point))
                    {
                        questGiverSpawns.Add(new PrefabSpawnPoint(point, prefabIndices[i]));
                    }
                }
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
                AddGroupMembers(random, config, tier, placed, placed.Count - 1, enemyPrefabs.Length,
                    enemySpawns);
            }

            // Ресурсы лежат кучками, а не ровным слоем: пустота между ними и делает
            // кучку находкой. `resourceCount` поэтому считает кучки.
            var lottery = SectorResources.Lottery(config, sector);
            var resourceSpawns = new List<PrefabSpawnPoint>();
            for (var i = 0; i < resourceCount; i++)
            {
                if (random.NextDouble() > config.ResourceSpawnChance || lottery.Count == 0)
                {
                    continue;
                }

                if (!TryPlace(random, config, placed, config.ResourceClearance, false, out var point))
                {
                    continue;
                }

                // Жребий бросается один раз на кучку: она целиком одного вида.
                var prefabIndex = lottery[random.Next(lottery.Count)];
                resourceSpawns.Add(new PrefabSpawnPoint(point, prefabIndex));
                AddClusterMembers(random, config, placed, placed.Count - 1, prefabIndex, resourceSpawns);
            }

            return new ZoneContent(resourceSpawns, enemySpawns, traderSpawn, questGiverSpawns,
                wormholeSpawn, islandSpawns, PlacePuzzles(random, config, islandSpawns));
        }

        // Находки стоят у края островка: шахта вгрызлась в породу, спутник о неё
        // разбился. Место под них не проверяется — кольцо islandMargin вокруг самого
        // островка шире, чем отступ находки, так что там никого и не может быть.
        private static List<PrefabSpawnPoint> PlacePuzzles(System.Random random, ZoneConfig config,
            List<IslandSpawnPoint> islands)
        {
            var spawns = new List<PrefabSpawnPoint>();
            if (HasNoPrefabs(config.PuzzlePrefabs) || islands.Count == 0)
            {
                return spawns;
            }

            // Островок на находку один, и две находки в секторе разные: номер
            // префаба каждой достаётся свой, как у квестодателей.
            var islandOrder = ShuffledIndices(random, islands.Count);
            var prefabIndices = ShuffledIndices(random, config.PuzzlePrefabs.Length);
            var count = Mathf.Min(Mathf.Min(config.PuzzleCount, islandOrder.Count), prefabIndices.Count);

            for (var i = 0; i < count; i++)
            {
                if (random.NextDouble() > config.PuzzleSpawnChance)
                {
                    continue;
                }

                var island = islands[islandOrder[i]];
                var radius = AsteroidIsland.WorldRadiusFor(island.Position, config.IslandRadiusTiles)
                             + Mathf.Max(0f, config.PuzzleIslandMargin);

                // Угол перебирается, а не зажимается к зоне: прижатая точка уехала бы
                // внутрь породы, где до находки уже не долететь.
                var attempts = Mathf.Max(1, config.PlacementAttempts);
                for (var attempt = 0; attempt < attempts; attempt++)
                {
                    var angle = (float)random.NextDouble() * Mathf.PI * 2f;
                    var point = island.Position +
                                new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

                    if (!InsideArea(point, config.AreaSize))
                    {
                        continue;
                    }

                    // Верх спрайта смотрит от островка: находка стоит на породе.
                    spawns.Add(new PrefabSpawnPoint(point, prefabIndices[i],
                        angle * Mathf.Rad2Deg - 90f));
                    break;
                }
            }

            return spawns;
        }

        private static bool InsideArea(Vector2 point, Vector2 areaSize) =>
            Mathf.Abs(point.x) <= areaSize.x * 0.5f && Mathf.Abs(point.y) <= areaSize.y * 0.5f;

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

        private static List<int> ShuffledIndices(System.Random random, int count)
        {
            var indices = new List<int>(count);
            for (var i = 0; i < count; i++)
            {
                indices.Add(i);
            }

            for (var i = count - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            return indices;
        }

        // Кучка целиком одного типа: обломки в ней отличаются только местом, и тогда
        // «поле железа» читается с одного взгляда, а не как случайная россыпь.
        private static void AddClusterMembers(System.Random random, ZoneConfig config,
            List<Placement> placed, int firstIndex, int prefabIndex, List<PrefabSpawnPoint> spawns)
        {
            var first = placed[firstIndex].Position;
            var size = RollSize(random, config.ResourceGroupSize);

            for (var i = 1; i < size; i++)
            {
                if (TryPlaceNear(random, config, placed, first, config.ResourceGroupRadius,
                        config.ResourceMemberClearance, firstIndex, out var point))
                {
                    spawns.Add(new PrefabSpawnPoint(point, prefabIndex));
                }
            }
        }

        private static int RollSize(System.Random random, Vector2Int range)
        {
            var min = Mathf.Max(1, range.x);
            var max = Mathf.Max(min, range.y);
            return random.Next(min, max + 1);
        }

        // Остальные в звене жмутся к вожаку: между собой у них свой тесный радиус,
        // а от чужих объектов их по-прежнему отталкивает больший из двух. Своего вожака
        // спутник мерит тем же тесным радиусом — его собственный `EnemyClearance`
        // больше, чем `GroupRadius`, и иначе звено никогда бы не собралось.
        private static void AddGroupMembers(System.Random random, ZoneConfig config, ZoneTier tier,
            List<Placement> placed, int leaderIndex, int prefabCount, List<PrefabSpawnPoint> spawns)
        {
            if (tier == null)
            {
                return;
            }

            var leader = placed[leaderIndex].Position;
            var size = tier.RollGroupSize(random);
            for (var i = 1; i < size; i++)
            {
                if (TryPlaceNear(random, config, placed, leader, tier.GroupRadius,
                        config.GroupMemberClearance, leaderIndex, out var point))
                {
                    spawns.Add(new PrefabSpawnPoint(point, random.Next(prefabCount)));
                }
            }
        }

        private static bool TryPlaceNear(System.Random random, ZoneConfig config, List<Placement> placed,
            Vector2 center, float radius, float clearance, int leaderIndex, out Vector2 point)
        {
            var attempts = Mathf.Max(1, config.PlacementAttempts);
            for (var i = 0; i < attempts; i++)
            {
                var candidate = ClampToArea(center + RandomInCircle(random, radius), config.AreaSize);
                if (Gap(candidate, clearance, config, placed, leaderIndex) >= 0f)
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
        // `ownLeaderIndex` — вожак того же звена: от него кандидату хватает своего
        // тесного радиуса.
        private static float Gap(Vector2 candidate, float clearance, ZoneConfig config,
            List<Placement> placed, int ownLeaderIndex = -1)
        {
            // Игрок после прыжка оказывается в центре зоны, поэтому там тоже пусто.
            var gap = candidate.magnitude - config.PlayerClearRadius;

            for (var i = 0; i < placed.Count; i++)
            {
                var other = placed[i];
                var keepOut = i == ownLeaderIndex ? clearance : Mathf.Max(clearance, other.Clearance);
                var distance = Vector2.Distance(candidate, other.Position) - keepOut;
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
