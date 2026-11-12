using Game.Core;
using TMPro;
using UnityEngine;

namespace Game.Gameplay.Flight
{
    // Подсказка "[E] Trade" над тем, с чем сработает клавиша. Содержимое зоны
    // генерируется, настраивать подсказку в префабах негде — ярлык собирается кодом
    // и переезжает к очередной цели, оставаясь одним объектом на весь полёт.
    public sealed class InteractPromptLabel
    {
        // Слой UI: на нём же висят остальные мировые канвасы космоса.
        private const int UiLayer = 5;

        private readonly Transform _parent;
        private readonly TMP_FontAsset _font;
        private readonly float _fontSize;
        private readonly Color _color;
        private readonly Vector2 _size;
        private readonly float _margin;
        private readonly int _sortingOrder;
        private readonly string _keyLabel;

        private GameObject _root;
        private TMP_Text _text;
        private Collider2D _target;
        private IInteractable _interactable;
        private string _shownPrompt;

        public InteractPromptLabel(Transform parent, TMP_FontAsset font, float fontSize, Color color,
            Vector2 size, float margin, int sortingOrder, string keyLabel)
        {
            _parent = parent;
            _font = font;
            _fontSize = fontSize;
            _color = color;
            _size = size;
            _margin = margin;
            _sortingOrder = sortingOrder;
            _keyLabel = keyLabel;
        }

        public void Focus(Collider2D target, IInteractable interactable)
        {
            _target = target;
            _interactable = interactable;
        }

        public void Hide()
        {
            _target = null;
            _interactable = null;

            if (_root != null && _root.activeSelf)
            {
                _root.SetActive(false);
            }
        }

        // Ярлык висит над целью, а не на ней: корабль вертится, подсказка — нет.
        // Позиция берётся из коллайдера, по которому идёт и само взаимодействие,
        // поэтому над крупным кораблём подсказка поднимается сама.
        public void Follow()
        {
            if (_target == null || _interactable == null)
            {
                Hide();
                return;
            }

            if (_root == null)
            {
                Build();
            }

            if (_shownPrompt != _interactable.Prompt)
            {
                _shownPrompt = _interactable.Prompt;
                _text.text = $"[{_keyLabel}] {_shownPrompt}";
            }

            var bounds = _target.bounds;
            _root.transform.position = new Vector3(bounds.center.x, bounds.max.y + _margin, 0f);

            if (!_root.activeSelf)
            {
                _root.SetActive(true);
            }
        }

        private void Build()
        {
            _root = new GameObject("InteractPrompt", typeof(RectTransform)) { layer = UiLayer };
            _root.transform.SetParent(_parent, false);

            var rect = (RectTransform)_root.transform;
            rect.sizeDelta = _size;

            var canvas = _root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = _sortingOrder;

            var holder = new GameObject("Text", typeof(RectTransform)) { layer = UiLayer };
            holder.transform.SetParent(_root.transform, false);

            var textRect = (RectTransform)holder.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _text = holder.AddComponent<TextMeshProUGUI>();
            if (_font != null)
            {
                _text.font = _font;
            }

            _text.fontSize = _fontSize;
            _text.color = _color;
            _text.alignment = TextAlignmentOptions.Center;
            _text.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }
}
