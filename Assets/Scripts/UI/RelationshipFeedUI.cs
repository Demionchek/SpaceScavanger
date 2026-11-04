using System.Collections.Generic;
using Game.Core;
using TMPro;
using UnityEngine;
using VContainer;

namespace Game.UI
{
    // Изменение отношений прямо в окне диалога: баннеры HUD там не годятся —
    // в диалоге HUD спрятан, да и канвас диалога рисуется поверх. Вешается на
    // объект под канвасом диалога, панель строится кодом.
    public sealed class RelationshipFeedUI : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private float _fontSize = 56f;
        [SerializeField] private Color _positive = new(0.45f, 0.9f, 0.45f);
        [SerializeField] private Color _negative = new(0.95f, 0.45f, 0.4f);
        [SerializeField] private Vector2 _position = new(340f, 440f);
        [SerializeField] private float _holdSeconds = 2.5f;
        [SerializeField] private float _fadeSeconds = 0.5f;

        private readonly Dictionary<CharacterDefinition, int> _last = new();

        private EventBus _eventBus;
        private TMP_Text _text;
        private float _timer;

        [Inject]
        public void Construct(EventBus eventBus)
        {
            _eventBus = eventBus;
            _eventBus.Subscribe<RelationshipChangedEvent>(OnRelationshipChanged);
            _eventBus.Subscribe<GameLoadStartedEvent>(OnLoadStarted);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<RelationshipChangedEvent>(OnRelationshipChanged);
            _eventBus?.Unsubscribe<GameLoadStartedEvent>(OnLoadStarted);
        }

        private void Awake()
        {
            Build();
        }

        // Диалог идёт на паузе, поэтому время только нескалированное.
        private void Update()
        {
            if (_timer <= 0f)
            {
                return;
            }

            _timer -= Time.unscaledDeltaTime;

            var alpha = _fadeSeconds > 0f ? Mathf.Clamp01(_timer / _fadeSeconds) : 1f;
            var color = _text.color;
            color.a = alpha;
            _text.color = color;

            if (_timer <= 0f)
            {
                _text.gameObject.SetActive(false);
            }
        }

        private void OnLoadStarted(GameLoadStartedEvent _) => _last.Clear();

        private void OnRelationshipChanged(RelationshipChangedEvent evt)
        {
            if (evt.Character == null)
            {
                return;
            }

            var delta = evt.NewValue - (_last.TryGetValue(evt.Character, out var previous) ? previous : 0);
            _last[evt.Character] = evt.NewValue;

            if (delta == 0)
            {
                return;
            }

            var name = string.IsNullOrEmpty(evt.Character.DisplayName) ? evt.Character.name : evt.Character.DisplayName;
            _text.text = delta > 0 ? $"{name} +{delta}" : $"{name} {delta}";
            _text.color = delta > 0 ? _positive : _negative;
            _text.gameObject.SetActive(true);
            _timer = Mathf.Max(_fadeSeconds, _holdSeconds);
        }

        private void Build()
        {
            var holder = new GameObject("Change", typeof(RectTransform));
            holder.transform.SetParent(transform, false);

            var rect = (RectTransform)holder.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = _position;
            rect.sizeDelta = new Vector2(800f, 80f);

            _text = holder.AddComponent<TextMeshProUGUI>();
            if (_font != null)
            {
                _text.font = _font;
            }

            _text.fontSize = _fontSize;
            _text.alignment = TextAlignmentOptions.BottomLeft;
            _text.textWrappingMode = TextWrappingModes.NoWrap;
            holder.SetActive(false);
        }
    }
}
