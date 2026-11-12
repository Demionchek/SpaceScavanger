using System.Text;
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

namespace Game.UI
{
    // Список управления в углу экрана. Клавиши берутся из самого Input Actions,
    // поэтому легенда не врёт после перепривязки. Панель собирается кодом: в сцене
    // нужен только пустой объект со шрифтом.
    public sealed class ControlsLegendUI : MonoBehaviour
    {
        [SerializeField] private TMP_FontAsset _font;
        [SerializeField] private float _fontSize = 44f;
        [SerializeField] private Color _textColor = Color.white;
        [SerializeField] private Color _background = new(0f, 0f, 0f, 0.55f);
        [SerializeField] private Vector2 _corner = new(40f, 40f);
        [Tooltip("Отступы панели от текста: по горизонтали и по вертикали.")]
        [SerializeField] private Vector2 _padding = new(28f, 18f);
        [Tooltip("F1 занята рубильником отладочных клавиш.")]
        [SerializeField] private Key _toggleKey = Key.F2;
        [SerializeField] private bool _visibleByDefault = true;

        private readonly StringBuilder _builder = new();

        private EventBus _eventBus;
        private HudVisibility _hud;
        private InputActionAsset _inputActions;
        private GameObject _panel;
        private TMP_Text _text;
        private bool _wanted;

        [Inject]
        public void Construct(EventBus eventBus, HudVisibility hud, InputActionAsset inputActions)
        {
            _eventBus = eventBus;
            _hud = hud;
            _inputActions = inputActions;
        }

        private void Start()
        {
            _wanted = _visibleByDefault;
            Build();
            _eventBus.Subscribe<HudVisibilityChangedEvent>(OnHudVisibilityChanged);
            Apply();
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<HudVisibilityChangedEvent>(OnHudVisibilityChanged);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard[_toggleKey].wasPressedThisFrame)
            {
                _wanted = !_wanted;
                Apply();
            }
        }

        private void OnHudVisibilityChanged(HudVisibilityChangedEvent _) => Apply();

        private void Apply()
        {
            if (_panel != null)
            {
                _panel.SetActive(_wanted && _hud.IsVisible);
            }
        }

        private void Build()
        {
            _panel = new GameObject("Legend", typeof(RectTransform));
            _panel.transform.SetParent(transform, false);

            var rect = (RectTransform)_panel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = _corner;

            _panel.AddComponent<Image>().color = _background;

            var layout = _panel.AddComponent<VerticalLayoutGroup>();
            var horizontal = Mathf.RoundToInt(_padding.x);
            var vertical = Mathf.RoundToInt(_padding.y);
            layout.padding = new RectOffset(horizontal, horizontal, vertical, vertical);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = _panel.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var textHolder = new GameObject("Text", typeof(RectTransform));
            textHolder.transform.SetParent(_panel.transform, false);

            _text = textHolder.AddComponent<TextMeshProUGUI>();
            if (_font != null)
            {
                _text.font = _font;
            }

            _text.fontSize = _fontSize;
            _text.color = _textColor;
            _text.alignment = TextAlignmentOptions.TopLeft;
            _text.textWrappingMode = TextWrappingModes.NoWrap;
            _text.text = BuildText();
        }

        private string BuildText()
        {
            _builder.Clear();
            var flight = _inputActions.FindActionMap("Flight", throwIfNotFound: false);

            Line("Thrust / brake", flight, "Throttle");
            Line("Turn", flight, "Rotation");
            Line("Fire", flight, "Fire");
            Line("Hook", flight, "Hook");
            Line("Use / board ship", flight, "Interact");
            _builder.Append("Inventory  I\nJournal  J");

            return _builder.ToString();
        }

        private void Line(string label, InputActionMap map, string action)
        {
            var keys = InputKeyLabels.For(map?.FindAction(action, throwIfNotFound: false));
            if (string.IsNullOrEmpty(keys))
            {
                return;
            }

            _builder.Append(label).Append("  ").Append(keys).Append('\n');
        }
    }
}
