using System.Collections.Generic;
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using VContainer;

namespace Game.Gameplay.Ship
{
    // Подсказка и подсветка над интерактивным объектом интерьера — видны, пока
    // игрок ни разу им не воспользовался. Вешается рядом с IInteractable.
    public sealed class InteractionHint : MonoBehaviour
    {
        [Tooltip("Уникальный ключ для сейва. Пусто — имя объекта.")]
        [SerializeField] private string _id;
        [SerializeField] private GameObject _hintRoot;
        [SerializeField] private TMP_Text _hintText;
        [Tooltip("Light2D и/или спрайт свечения — пульсируют, пока подсказка видна.")]
        [SerializeField] private GameObject _highlight;
        [SerializeField] private float _pulseSpeed = 3f;
        [SerializeField, Range(0f, 1f)] private float _pulseMin = 0.4f;

        private readonly List<Light2D> _lights = new();
        private readonly List<float> _lightIntensities = new();
        private readonly List<SpriteRenderer> _sprites = new();
        private readonly List<float> _spriteAlphas = new();

        private IInteractionHintService _hints;
        private IInteractable _interactable;
        private string _keyLabel;
        private string _shownPrompt;

        private string Id => string.IsNullOrEmpty(_id) ? gameObject.name : _id;

        [Inject]
        public void Construct(IInteractionHintService hints, InputActionAsset inputActions)
        {
            _hints = hints;
            _keyLabel = inputActions.FindAction("Platformer/Interact", throwIfNotFound: true).GetBindingDisplayString();
        }

        private void Awake()
        {
            _interactable = GetComponent<IInteractable>();
            if (_interactable == null)
            {
                Debug.LogWarning($"InteractionHint '{name}': рядом нет IInteractable", this);
            }

            if (_highlight == null)
            {
                return;
            }

            foreach (var light in _highlight.GetComponentsInChildren<Light2D>(true))
            {
                _lights.Add(light);
                _lightIntensities.Add(light.intensity);
            }

            foreach (var sprite in _highlight.GetComponentsInChildren<SpriteRenderer>(true))
            {
                _sprites.Add(sprite);
                _spriteAlphas.Add(sprite.color.a);
            }
        }

        private void Start()
        {
            SetVisible(_hints != null && !_hints.IsUsed(Id));
        }

        private void Update()
        {
            if (_interactable != null && _hintText != null && _hintRoot != null && _hintRoot.activeSelf
                && _shownPrompt != _interactable.Prompt)
            {
                _shownPrompt = _interactable.Prompt;
                _hintText.text = $"[{_keyLabel}] {_shownPrompt}";
            }

            if (_highlight == null || !_highlight.activeSelf)
            {
                return;
            }

            var pulse = Mathf.Lerp(_pulseMin, 1f, (Mathf.Sin(Time.unscaledTime * _pulseSpeed) + 1f) * 0.5f);

            for (var i = 0; i < _lights.Count; i++)
            {
                _lights[i].intensity = _lightIntensities[i] * pulse;
            }

            for (var i = 0; i < _sprites.Count; i++)
            {
                var color = _sprites[i].color;
                color.a = _spriteAlphas[i] * pulse;
                _sprites[i].color = color;
            }
        }

        public void MarkUsed()
        {
            _hints?.MarkUsed(Id);
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            if (_hintRoot != null)
            {
                _hintRoot.SetActive(visible);
            }

            if (_highlight != null)
            {
                _highlight.SetActive(visible);
            }
        }
    }
}
