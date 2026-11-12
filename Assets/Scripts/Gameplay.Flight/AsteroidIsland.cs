using System.Collections.Generic;
using Game.Core;
using Game.Data;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.Gameplay.Flight
{
    // Островок из тайлов: форма — круг, обкусанный шумом, тайл выбирается по
    // маске соседей, коллайдер собирается из самих тайлов. Всё строится в рантайме,
    // поэтому префаб не нужен, а форма повторяется от того же зерна.
    public sealed class AsteroidIsland : MonoBehaviour
    {
        private const float CellSize = 0.64f;

        // Шаг точек контура на радаре: реже — периметр рассыпается, чаще — сливается
        // в линию и съедает пул точек миникарты.
        private const float OutlineStep = 1.3f;

        private const int Full = SpaceTileSet.North | SpaceTileSet.East
                                 | SpaceTileSet.South | SpaceTileSet.West;

        private Tilemap _ground;
        private Tilemap _details;
        private IslandOutline _outline;

        public float WorldRadius { get; private set; }

        // Зерно и размер выводятся из позиции: сохранять их отдельно не нужно,
        // загрузка восстановит ту же форму по той же точке.
        public static int SeedFor(Vector2 position)
        {
            unchecked
            {
                var x = Mathf.RoundToInt(position.x * 100f);
                var y = Mathf.RoundToInt(position.y * 100f);
                var hash = (uint)(x * 73856093) ^ (uint)(y * 19349663);
                hash ^= hash >> 13;
                hash *= 0x5bd1e995;
                hash ^= hash >> 15;
                return (int)(hash | 1u);
            }
        }

        public static int RadiusFor(Vector2 position, Vector2Int range)
        {
            var min = Mathf.Max(2, Mathf.Min(range.x, range.y));
            var max = Mathf.Max(min, Mathf.Max(range.x, range.y));
            var state = (uint)SeedFor(position);
            return min + (int)(NextFloat(ref state) * (max - min + 1));
        }

        public static float WorldRadiusFor(Vector2 position, Vector2Int range) =>
            (RadiusFor(position, range) + 1) * CellSize;

        public void Build(SpaceTileSet set, int radiusTiles, int seed, int layer,
            int groundOrder, int detailOrder, float detailChance)
        {
            if (set == null)
            {
                return;
            }

            radiusTiles = Mathf.Clamp(radiusTiles, 2, 64);
            WorldRadius = (radiusTiles + 1) * CellSize;

            EnsureTilemaps(layer, groundOrder, detailOrder);

            var shape = Shape(radiusTiles, seed);
            var state = (uint)(seed == 0 ? 1 : seed);

            foreach (var cell in shape)
            {
                var mask = Mask(shape, cell);
                var tile = set.Resolve(mask);

                if (tile == null && mask == Full)
                {
                    tile = set.ResolveInner(
                        shape.Contains(new Vector2Int(cell.x - 1, cell.y + 1)),
                        shape.Contains(new Vector2Int(cell.x + 1, cell.y + 1)),
                        shape.Contains(new Vector2Int(cell.x - 1, cell.y - 1)),
                        shape.Contains(new Vector2Int(cell.x + 1, cell.y - 1)));
                }

                if (tile == null)
                {
                    tile = Pick(set.Fill, ref state);
                }

                _ground.SetTile(new Vector3Int(cell.x, cell.y, 0), tile);

                // Детали только в глубине: на краю они торчали бы за породу.
                if (mask == Full && set.Details != null && set.Details.Length > 0
                    && NextFloat(ref state) < detailChance)
                {
                    _details.SetTile(new Vector3Int(cell.x, cell.y, 0), Pick(set.Details, ref state));
                }
            }

            // Контур живёт на объекте с коллайдером: радар доходит до островка
            // сканированием физики, а дальше читает точки с найденного объекта.
            _outline = _ground.gameObject.AddComponent<IslandOutline>();
            _outline.Set(Outline(shape));
        }

        // Точка на породе в сторону towards, плюс отступ наружу. Берётся с того же
        // контура, что рисует радар: формульный радиус — это внешняя граница всех
        // клеток, а настоящий силуэт после обрезки куда меньше, и находка у него
        // висела бы в пустоте. Угол возвращается готовым поворотом спрайта: верх
        // смотрит от островка, то есть находка стоит на поверхности.
        public bool TrySurfacePoint(Vector2 towards, float standoff, out Vector2 point,
            out float angleDegrees)
        {
            point = towards;
            angleDegrees = 0f;

            if (_outline == null || _outline.Points.Count == 0)
            {
                return false;
            }

            var center = (Vector2)transform.position;
            var wanted = towards - center;
            wanted = wanted.sqrMagnitude > 0.0001f ? wanted.normalized : Vector2.up;

            var best = Vector2.zero;
            var bestDot = float.NegativeInfinity;
            foreach (var candidate in _outline.Points)
            {
                var offset = candidate - center;
                if (offset.sqrMagnitude < 0.0001f)
                {
                    continue;
                }

                var dot = Vector2.Dot(offset.normalized, wanted);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    best = candidate;
                }
            }

            if (bestDot == float.NegativeInfinity)
            {
                return false;
            }

            var outward = (best - center).normalized;
            point = best + outward * standoff;
            angleDegrees = Mathf.Atan2(outward.y, outward.x) * Mathf.Rad2Deg - 90f;
            return true;
        }

        // По одной точке на угловой сектор — самая дальняя кромка в нём. Контур
        // снимается с настоящих клеток, а не с формулы края: обрезка тонких клеток
        // сдвигает силуэт на пару тайлов, и на радаре это было бы видно.
        private Vector2[] Outline(HashSet<Vector2Int> shape)
        {
            var sectors = Mathf.Clamp(
                Mathf.RoundToInt(2f * Mathf.PI * WorldRadius / OutlineStep), 8, 32);
            var best = new Vector2[sectors];
            var bestSqr = new float[sectors];

            foreach (var cell in shape)
            {
                if (Mask(shape, cell) == Full)
                {
                    continue;
                }

                var local = new Vector2((cell.x + 0.5f) * CellSize, (cell.y + 0.5f) * CellSize);
                var angle = Mathf.Atan2(local.y, local.x) + Mathf.PI;
                var index = Mathf.Clamp((int)(angle / (Mathf.PI * 2f) * sectors), 0, sectors - 1);

                var sqr = local.sqrMagnitude;
                if (sqr > bestSqr[index])
                {
                    bestSqr[index] = sqr;
                    best[index] = local;
                }
            }

            var origin = (Vector2)transform.position;
            var points = new List<Vector2>(sectors);
            for (var i = 0; i < sectors; i++)
            {
                if (bestSqr[i] > 0f)
                {
                    points.Add(origin + best[i]);
                }
            }

            return points.ToArray();
        }

        private void EnsureTilemaps(int layer, int groundOrder, int detailOrder)
        {
            // ?? мимо перегруженного Unity-оператора ==: GetComponent отдаёт
            // объект-пустышку, и его бы приняли за живой компонент.
            if (!TryGetComponent<Grid>(out var grid))
            {
                grid = gameObject.AddComponent<Grid>();
            }

            grid.cellSize = new Vector3(CellSize, CellSize, 0f);

            _ground = CreateLayer("Ground", layer, groundOrder);

            var body = _ground.gameObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;

            var composite = _ground.gameObject.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

            var collider = _ground.gameObject.AddComponent<TilemapCollider2D>();
            collider.compositeOperation = Collider2D.CompositeOperation.Merge;

            _details = CreateLayer("Details", layer, detailOrder);
        }

        private Tilemap CreateLayer(string name, int layer, int sortingOrder)
        {
            var holder = new GameObject(name) { layer = layer };
            holder.transform.SetParent(transform, false);

            var map = holder.AddComponent<Tilemap>();
            holder.AddComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            return map;
        }

        // Круг радиуса R, у которого край гуляет по синусам от зерна, плюс заливка
        // дыр: одиночные пустоты внутри давали бы тайлы, которых нет в наборе.
        private static HashSet<Vector2Int> Shape(int radius, int seed)
        {
            var state = (uint)(seed == 0 ? 1 : seed);
            var phaseA = NextFloat(ref state) * Mathf.PI * 2f;
            var phaseB = NextFloat(ref state) * Mathf.PI * 2f;
            var waveA = 0.18f + NextFloat(ref state) * 0.22f;
            var waveB = 0.1f + NextFloat(ref state) * 0.15f;

            var cells = new HashSet<Vector2Int>();
            for (var y = -radius; y <= radius; y++)
            {
                for (var x = -radius; x <= radius; x++)
                {
                    var distance = Mathf.Sqrt(x * x + y * y);
                    if (distance < 0.001f)
                    {
                        cells.Add(new Vector2Int(x, y));
                        continue;
                    }

                    var angle = Mathf.Atan2(y, x);
                    var edge = radius * (1f - waveA * Mathf.Sin(angle * 3f + phaseA)
                                            - waveB * Mathf.Sin(angle * 5f + phaseB));

                    if (distance <= edge)
                    {
                        cells.Add(new Vector2Int(x, y));
                    }
                }
            }

            Cleanup(cells, radius);
            return cells;
        }

        // Набор тайлов не знает ни дыр, ни перемычек в одну клетку, поэтому форму
        // доводим до «толстой»: заливаем всё, куда не дотягивается внешняя пустота,
        // и срезаем клетки, для которых картинки нет.
        private static void Cleanup(HashSet<Vector2Int> cells, int radius)
        {
            for (var pass = 0; pass < 8; pass++)
            {
                var changed = FillEnclosed(cells, radius);
                changed |= TrimThin(cells);

                if (!changed)
                {
                    return;
                }
            }
        }

        private static bool FillEnclosed(HashSet<Vector2Int> cells, int radius)
        {
            var bound = radius + 2;
            var outside = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            var start = new Vector2Int(-bound, -bound);

            outside.Add(start);
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                for (var i = 0; i < 4; i++)
                {
                    var next = Neighbour(cell, i);
                    if (next.x < -bound || next.x > bound || next.y < -bound || next.y > bound ||
                        cells.Contains(next) || !outside.Add(next))
                    {
                        continue;
                    }

                    queue.Enqueue(next);
                }
            }

            var changed = false;
            for (var y = -bound; y <= bound; y++)
            {
                for (var x = -bound; x <= bound; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!cells.Contains(cell) && !outside.Contains(cell))
                    {
                        cells.Add(cell);
                        changed = true;
                    }
                }
            }

            return changed;
        }

        private static bool TrimThin(HashSet<Vector2Int> cells)
        {
            if (cells.Count <= 9)
            {
                return false;
            }

            var doomed = new List<Vector2Int>();
            foreach (var cell in cells)
            {
                var mask = Mask(cells, cell);
                var thin = mask == 0
                           || mask == SpaceTileSet.North || mask == SpaceTileSet.East
                           || mask == SpaceTileSet.South || mask == SpaceTileSet.West
                           || mask == (SpaceTileSet.North | SpaceTileSet.South)
                           || mask == (SpaceTileSet.East | SpaceTileSet.West);

                if (thin)
                {
                    doomed.Add(cell);
                }
            }

            foreach (var cell in doomed)
            {
                cells.Remove(cell);
            }

            return doomed.Count > 0;
        }

        private static Vector2Int Neighbour(Vector2Int cell, int direction) => direction switch
        {
            0 => new Vector2Int(cell.x, cell.y + 1),
            1 => new Vector2Int(cell.x + 1, cell.y),
            2 => new Vector2Int(cell.x, cell.y - 1),
            _ => new Vector2Int(cell.x - 1, cell.y)
        };

        private static int Mask(HashSet<Vector2Int> cells, Vector2Int cell)
        {
            var mask = 0;
            if (cells.Contains(new Vector2Int(cell.x, cell.y + 1))) mask |= SpaceTileSet.North;
            if (cells.Contains(new Vector2Int(cell.x + 1, cell.y))) mask |= SpaceTileSet.East;
            if (cells.Contains(new Vector2Int(cell.x, cell.y - 1))) mask |= SpaceTileSet.South;
            if (cells.Contains(new Vector2Int(cell.x - 1, cell.y))) mask |= SpaceTileSet.West;
            return mask;
        }

        private static TileBase Pick(IReadOnlyList<TileBase> tiles, ref uint state)
        {
            if (tiles == null || tiles.Count == 0)
            {
                return null;
            }

            return tiles[Mathf.Min((int)(NextFloat(ref state) * tiles.Count), tiles.Count - 1)];
        }

        private static float NextFloat(ref uint state)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            return (state & 0xFFFFFF) / (float)0x1000000;
        }
    }
}
