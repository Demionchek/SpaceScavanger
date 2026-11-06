using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.UI
{
    public sealed class MinimapUI : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private RectTransform _radarArea;
        [SerializeField] private Image _blipPrefab;
        [SerializeField] private int _maxBlips = 64;
        [SerializeField] private float _baseRange = 20f;
        [SerializeField] private float _scanInterval = 0.2f;
        [SerializeField] private LayerMask _resourceMask;
        [SerializeField] private LayerMask _enemyMask;
        [SerializeField] private LayerMask _npcMask;
        [SerializeField] private Color _resourceColor = new(0.3f, 0.9f, 1f, 1f);
        [SerializeField] private Color _enemyColor = Color.red;
        [SerializeField] private Color _npcColor = Color.yellow;
        [SerializeField] private Color _boundaryColor = new(1f, 0.4f, 0.1f, 1f);
        [SerializeField] private int _boundarySamples = 48;

        [Header("Запомненные объекты (RadarLandmark)")]
        [SerializeField] private int _maxLandmarks = 8;
        [SerializeField] private float _landmarkSize = 34f;

        [Tooltip("Насколько близко к краю радара прижимается метка вне дальности.")]
        [SerializeField] private float _landmarkEdgeInset = 0.86f;

        [Tooltip("Прозрачность метки, пока объект вне дальности радара.")]
        [SerializeField] private float _offRangeAlpha = 0.7f;

        [Tooltip("Указатель направления вне дальности. Спрайт должен смотреть вверх.")]
        [SerializeField] private Sprite _directionArrow;

        [SerializeField] private float _directionArrowSize = 18f;

        private IShipStatsService _stats;
        private IPlayerLocator _player;
        private ZoneBounds _bounds;
        private EventBus _eventBus;

        private struct Detected
        {
            public Transform Transform;
            public Color Color;
        }

        private readonly List<Image> _blips = new();
        private readonly List<Image> _boundaryBlips = new();
        private readonly List<Collider2D> _hits = new();
        private readonly HashSet<Transform> _seen = new();
        private readonly List<Detected> _detected = new();
        private readonly List<RadarLandmark> _landmarks = new();
        private readonly List<Image> _landmarkIcons = new();
        private readonly List<Image> _landmarkArrows = new();
        private ContactFilter2D _filter;
        private float _scanTimer;

        private GameObject Root => _root != null ? _root : gameObject;

        [Inject]
        public void Construct(IShipStatsService stats, IPlayerLocator player, ZoneBounds bounds, EventBus eventBus,
            HudVisibility hud)
        {
            _stats = stats;
            _player = player;
            _bounds = bounds;
            _eventBus = eventBus;

            // Дальность радара живёт здесь, поэтому экран состояния узнаёт её отсюда.
            stats.SetBase(ShipStat.RadarRange, _baseRange);

            _eventBus.Subscribe<HudVisibilityChangedEvent>(OnHudVisibilityChanged);
            Root.SetActive(hud.IsVisible);
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

            for (var i = 0; i < _boundarySamples; i++)
            {
                var blip = Instantiate(_blipPrefab, _radarArea);
                blip.color = _boundaryColor;
                blip.enabled = false;
                _boundaryBlips.Add(blip);
            }

            // Иконки создаются последними, чтобы рисоваться поверх точек.
            for (var i = 0; i < _maxLandmarks; i++)
            {
                _landmarkArrows.Add(NewLandmarkImage(_directionArrowSize));
                _landmarkIcons.Add(NewLandmarkImage(_landmarkSize));
            }
        }

        private Image NewLandmarkImage(float size)
        {
            var image = Instantiate(_blipPrefab, _radarArea);
            image.rectTransform.sizeDelta = new Vector2(size, size);
            image.enabled = false;
            return image;
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<HudVisibilityChangedEvent>(OnHudVisibilityChanged);
        }

        private void OnHudVisibilityChanged(HudVisibilityChangedEvent evt) => Root.SetActive(evt.Visible);

        private void LateUpdate()
        {
            _scanTimer -= Time.deltaTime;
            if (_scanTimer <= 0f)
            {
                _scanTimer = _scanInterval;
                Scan();
            }

            Render();
            RenderLandmarks();
            RenderBoundary();
        }

        private void Scan()
        {
            var center = _player.Position;
            var count = Physics2D.OverlapCircle(center, CurrentRange(), _filter, _hits);

            _detected.Clear();
            _seen.Clear();

            for (var i = 0; i < count && _detected.Count < _blips.Count; i++)
            {
                var collider = _hits[i];
                if (collider == null)
                {
                    continue;
                }

                var entity = collider.attachedRigidbody != null
                    ? collider.attachedRigidbody.transform
                    : collider.transform;

                if (!_seen.Add(entity))
                {
                    continue;
                }

                // Торговец, квест-гивер и червоточина получают свою иконку вместо точки,
                // и радар их запоминает — дальше метка держится сама.
                if (entity.TryGetComponent<RadarLandmark>(out var landmark))
                {
                    Discover(landmark);
                }
                else if (TryClassify(collider.gameObject.layer, out var color))
                {
                    _detected.Add(new Detected { Transform = entity, Color = color });
                }
            }
        }

        private void Discover(RadarLandmark landmark)
        {
            if (_landmarks.Count < _landmarkIcons.Count && !_landmarks.Contains(landmark))
            {
                _landmarks.Add(landmark);
            }
        }

        private void Render()
        {
            var range = CurrentRange();
            var radarRadius = _radarArea.rect.width * 0.5f;
            var center = _player.Position;
            var blipIndex = 0;

            for (var i = 0; i < _detected.Count && blipIndex < _blips.Count; i++)
            {
                var entity = _detected[i].Transform;
                if (entity == null)
                {
                    continue;
                }

                var offset = ((Vector2)entity.position - center) / range * radarRadius;
                if (offset.sqrMagnitude > radarRadius * radarRadius)
                {
                    continue;
                }

                var blip = _blips[blipIndex++];
                blip.enabled = true;
                blip.color = _detected[i].Color;
                blip.rectTransform.anchoredPosition = offset;
            }

            for (; blipIndex < _blips.Count; blipIndex++)
            {
                _blips[blipIndex].enabled = false;
            }
        }

        // Найденный объект с радара больше не исчезает: внутри дальности метка стоит
        // на своём месте, снаружи — прижата к краю и показывает направление.
        private void RenderLandmarks()
        {
            var range = CurrentRange();
            var radarRadius = _radarArea.rect.width * 0.5f;
            var center = _player.Position;

            for (var i = _landmarks.Count - 1; i >= 0; i--)
            {
                if (_landmarks[i] == null)
                {
                    _landmarks.RemoveAt(i);
                }
            }

            var index = 0;

            for (; index < _landmarks.Count && index < _landmarkIcons.Count; index++)
            {
                var landmark = _landmarks[index];
                var offset = ((Vector2)landmark.transform.position - center) / range * radarRadius;
                var inRange = offset.sqrMagnitude <= radarRadius * radarRadius;
                var direction = offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector2.up;
                var tint = landmark.Tint;
                var color = inRange ? tint : new Color(tint.r, tint.g, tint.b, tint.a * _offRangeAlpha);

                var icon = _landmarkIcons[index];
                icon.enabled = true;
                icon.color = color;
                icon.rectTransform.anchoredPosition =
                    inRange ? offset : direction * (radarRadius * _landmarkEdgeInset);

                if (landmark.Icon != null)
                {
                    icon.sprite = landmark.Icon;
                }

                var arrow = _landmarkArrows[index];
                arrow.enabled = !inRange && _directionArrow != null;

                if (arrow.enabled)
                {
                    arrow.sprite = _directionArrow;
                    arrow.color = color;
                    arrow.rectTransform.anchoredPosition = direction * radarRadius;
                    arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f,
                        Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
                }
            }

            for (; index < _landmarkIcons.Count; index++)
            {
                _landmarkIcons[index].enabled = false;
                _landmarkArrows[index].enabled = false;
            }
        }

        private void RenderBoundary()
        {
            var range = CurrentRange();
            var radarRadius = _radarArea.rect.width * 0.5f;
            var center = _player.Position;
            var boundsCenter = _bounds.Center;

            for (var i = 0; i < _boundaryBlips.Count; i++)
            {
                var angle = i / (float)_boundaryBlips.Count * Mathf.PI * 2f;
                var worldPoint = boundsCenter + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * _bounds.SafeRadius;
                var offset = (worldPoint - center) / range * radarRadius;

                var blip = _boundaryBlips[i];
                if (offset.sqrMagnitude > radarRadius * radarRadius)
                {
                    blip.enabled = false;
                    continue;
                }

                blip.enabled = true;
                blip.rectTransform.anchoredPosition = offset;
            }
        }

        private float CurrentRange()
        {
            return Mathf.Max(0.01f,
                _baseRange * _stats.GetMultiplier(ShipStat.RadarRange) + _stats.GetBonus(ShipStat.RadarRange));
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
