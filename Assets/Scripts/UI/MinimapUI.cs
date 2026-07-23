using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.UI
{
    // HUD-радар в полёте: объекты в радиусе от игрока показываются цветными
    // блипами (ресурсы / враги / NPC — по маскам слоёв). Радиус радара —
    // прокачиваемый ShipStat.RadarRange. Радар ориентирован по миру (север
    // сверху). Скрывается внутри корабля (реагирует на GameStateChangedEvent).
    public sealed class MinimapUI : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _radarArea;
        [SerializeField] private Image _blipPrefab;
        [SerializeField] private int _maxBlips = 64;
        [SerializeField] private float _baseRange = 20f;
        [SerializeField] private LayerMask _resourceMask;
        [SerializeField] private LayerMask _enemyMask;
        [SerializeField] private LayerMask _npcMask;
        [SerializeField] private Color _resourceColor = new(0.3f, 0.9f, 1f, 1f);
        [SerializeField] private Color _enemyColor = Color.red;
        [SerializeField] private Color _npcColor = Color.yellow;

        private IShipStatsService _stats;
        private IPlayerLocator _player;
        private EventBus _eventBus;

        private readonly List<Image> _blips = new();
        private readonly List<Collider2D> _hits = new();
        private readonly HashSet<Transform> _seen = new();
        private ContactFilter2D _filter;

        private GameObject Root => _root != null ? _root : gameObject;

        [Inject]
        public void Construct(IShipStatsService stats, IPlayerLocator player, EventBus eventBus)
        {
            _stats = stats;
            _player = player;
            _eventBus = eventBus;

            // Подписка в Construct, чтобы поймать стартовое событие SpaceFlightState.
            _eventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void Awake()
        {
            _filter = new ContactFilter2D
            {
                useLayerMask = true,
                useTriggers = true,
                layerMask = _resourceMask | _enemyMask | _npcMask
            };

            for (var i = 0; i < _maxBlips; i++)
            {
                var blip = Instantiate(_blipPrefab, _radarArea);
                blip.enabled = false;
                _blips.Add(blip);
            }
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.NewState is ShipInteriorState)
            {
                Root.SetActive(false);
            }
            else if (evt.NewState is SpaceFlightState)
            {
                Root.SetActive(true);
            }
        }

        private void LateUpdate()
        {
            var range = Mathf.Max(0.01f,
                _baseRange * _stats.GetMultiplier(ShipStat.RadarRange) + _stats.GetBonus(ShipStat.RadarRange));
            var radarRadius = _radarArea.rect.width * 0.5f;
            var center = _player.Position;

            var count = Physics2D.OverlapCircle(center, range, _filter, _hits);
            _seen.Clear();
            var blipIndex = 0;

            for (var i = 0; i < count && blipIndex < _blips.Count; i++)
            {
                var collider = _hits[i];
                if (collider == null)
                {
                    continue;
                }

                // Сущность = тело (у кораблей есть Rigidbody2D), иначе сам объект
                // коллайдера. transform.root не годится: всё спавнится под
                // SpaceRoot.Content, поэтому root у всех один и тот же.
                var entity = collider.attachedRigidbody != null
                    ? collider.attachedRigidbody.transform
                    : collider.transform;

                if (!_seen.Add(entity) || !TryClassify(collider.gameObject.layer, out var color))
                {
                    continue;
                }

                var offset = ((Vector2)entity.position - center) / range * radarRadius;
                var blip = _blips[blipIndex++];
                blip.enabled = true;
                blip.color = color;
                blip.rectTransform.anchoredPosition = offset;
            }

            for (; blipIndex < _blips.Count; blipIndex++)
            {
                _blips[blipIndex].enabled = false;
            }
        }

        private bool TryClassify(int layer, out Color color)
        {
            var bit = 1 << layer;

            if ((_enemyMask.value & bit) != 0)
            {
                color = _enemyColor;
                return true;
            }

            if ((_npcMask.value & bit) != 0)
            {
                color = _npcColor;
                return true;
            }

            if ((_resourceMask.value & bit) != 0)
            {
                color = _resourceColor;
                return true;
            }

            color = default;
            return false;
        }
    }
}
