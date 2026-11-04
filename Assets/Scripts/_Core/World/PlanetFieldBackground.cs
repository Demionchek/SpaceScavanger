using UnityEngine;
using VContainer;

namespace Game.Core
{
    // Бесконечное поле планет вокруг камеры: решётка с шагом _cellSize, по одной
    // планете на ячейку, тайлы переиспользуются при выходе из виду. Содержимое
    // ячейки выводится из её координат, так что, вернувшись назад, игрок видит те
    // же планеты на тех же местах. Спрайты нарезаны по самой планете (0.45-1.4
    // единицы), поэтому шаг задаётся числом, а не размером спрайта.
    public sealed class PlanetFieldBackground : MonoBehaviour
    {
        [SerializeField] private Sprite[] _planets;

        [Tooltip("Пусто — берётся Camera.main.")]
        [SerializeField] private Camera _camera;

        [Tooltip("Шаг решётки в мировых единицах: кадр камеры — примерно 17.8 x 10.")]
        [SerializeField] private Vector2 _cellSize = new(9f, 5.5f);

        [Tooltip("Во сколько раз увеличивать спрайт планеты.")]
        [SerializeField] private Vector2 _scaleRange = new(1.5f, 3f);

        [Tooltip("Разброс внутри ячейки, доля её размера: 0 — строгая решётка.")]
        [Range(0f, 1f)] [SerializeField] private float _jitter = 0.6f;

        [Tooltip("Сколько ячеек держать в каждую сторону от центральной.")]
        [SerializeField] private int _gridRadius = 1;

        [Tooltip("Какая доля движения камеры достаётся планетам: 0 — приклеены к " +
                 "экрану, 1 — намертво стоят в мире.")]
        [Range(0f, 1f)] [SerializeField] private float _parallax = 0.15f;

        [Tooltip("Доля занятых ячеек: пустые дают прорехи в небе.")]
        [Range(0f, 1f)] [SerializeField] private float _fillChance = 0.8f;

        [SerializeField] private Vector2 _alphaRange = new(1f, 1f);
        [SerializeField] private int _sortingOrder = -90;
        [SerializeField] private string _sortingLayer = "Default";
        [SerializeField] private int _seed = 1;

        private SpriteRenderer[] _tiles;
        private Vector2Int[] _cells;
        private Vector2[] _offsets;
        private EventBus _eventBus;

        [Inject]
        public void Construct(EventBus eventBus)
        {
            _eventBus = eventBus;
            _eventBus.Subscribe<SectorChangedEvent>(OnSectorChanged);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<SectorChangedEvent>(OnSectorChanged);
        }

        // Новый сектор — другое небо: сдвигаем зерно и забываем, что лежало в ячейках.
        private void OnSectorChanged(SectorChangedEvent evt)
        {
            _seed = evt.Number * 7919 + 1;

            if (_cells == null)
            {
                return;
            }

            for (var i = 0; i < _cells.Length; i++)
            {
                _cells[i] = new Vector2Int(int.MinValue, int.MinValue);
            }
        }

        private void Awake()
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            var side = Mathf.Max(1, _gridRadius * 2 + 1);
            var count = side * side;

            _tiles = new SpriteRenderer[count];
            _cells = new Vector2Int[count];
            _offsets = new Vector2[count];

            for (var i = 0; i < count; i++)
            {
                var tile = new GameObject($"Planet {i}").AddComponent<SpriteRenderer>();
                tile.transform.SetParent(transform, false);
                tile.sortingLayerName = _sortingLayer;
                tile.sortingOrder = _sortingOrder;
                tile.enabled = false;

                _tiles[i] = tile;
                _cells[i] = new Vector2Int(int.MinValue, int.MinValue);
            }
        }

        private void LateUpdate()
        {
            if (_camera == null || _tiles == null)
            {
                return;
            }

            // Центр экрана в системе решётки и остаток, на который решётку несёт
            // вместе с камерой: вместе они и дают отставание планет.
            var camera = (Vector2)_camera.transform.position;
            var anchor = camera * _parallax;
            var carry = camera - anchor;

            var cellSize = new Vector2(Mathf.Max(0.1f, _cellSize.x), Mathf.Max(0.1f, _cellSize.y));
            var baseX = Mathf.RoundToInt(anchor.x / cellSize.x);
            var baseY = Mathf.RoundToInt(anchor.y / cellSize.y);
            var depth = transform.position.z;
            var index = 0;

            for (var dy = -_gridRadius; dy <= _gridRadius; dy++)
            {
                for (var dx = -_gridRadius; dx <= _gridRadius; dx++, index++)
                {
                    var cell = new Vector2Int(baseX + dx, baseY + dy);
                    if (_cells[index] != cell)
                    {
                        _cells[index] = cell;
                        Fill(index, cell);
                    }

                    var position = new Vector2(cell.x * cellSize.x, cell.y * cellSize.y)
                                   + _offsets[index] + carry;
                    _tiles[index].transform.position = new Vector3(position.x, position.y, depth);
                }
            }
        }

        private void Fill(int index, Vector2Int cell)
        {
            var tile = _tiles[index];
            var state = Hash(cell.x, cell.y, _seed);

            if (_planets == null || _planets.Length == 0 || NextFloat(ref state) > _fillChance)
            {
                tile.enabled = false;
                _offsets[index] = Vector2.zero;
                return;
            }

            tile.enabled = true;
            tile.sprite = _planets[Mathf.Min((int)(NextFloat(ref state) * _planets.Length), _planets.Length - 1)];

            var scale = Mathf.Lerp(_scaleRange.x, _scaleRange.y, NextFloat(ref state));
            tile.transform.localScale = new Vector3(scale, scale, 1f);
            tile.color = new Color(1f, 1f, 1f, Mathf.Lerp(_alphaRange.x, _alphaRange.y, NextFloat(ref state)));
            tile.flipX = NextFloat(ref state) < 0.5f;
            tile.flipY = NextFloat(ref state) < 0.5f;

            _offsets[index] = new Vector2(
                (NextFloat(ref state) - 0.5f) * _cellSize.x * _jitter,
                (NextFloat(ref state) - 0.5f) * _cellSize.y * _jitter);
        }

        private static uint Hash(int x, int y, int seed)
        {
            unchecked
            {
                var hash = (uint)(x * 73856093) ^ (uint)(y * 19349663) ^ (uint)(seed * 83492791);
                hash ^= hash >> 13;
                hash *= 0x5bd1e995;
                hash ^= hash >> 15;
                return hash == 0u ? 1u : hash;
            }
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
