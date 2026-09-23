using System.Collections.Generic;
using Game.Core;
using Game.Gameplay.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.UI
{
    public sealed class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private GameObject _barPrefab;
        [SerializeField] private Transform _barsContainer;
        [SerializeField] private TMP_Text _healthText;
        [SerializeField] private int _barCount = 10;
        [SerializeField] private GameObject _root;

        private readonly List<GameObject> _bars = new();
        private Health _health;
        private EventBus _eventBus;

        private GameObject Root => _root != null ? _root : gameObject;

        [Inject]
        public void Construct(Health health, EventBus eventBus, HudVisibility hud)
        {
            _health = health;
            _eventBus = eventBus;

            // Подписка в Construct, а не в OnEnable: первое событие видимости
            // приходит сразу после сборки контейнера, и сам Root может быть скрыт.
            _eventBus.Subscribe<HudVisibilityChangedEvent>(OnHudVisibilityChanged);
            Root.SetActive(hud.IsVisible);
        }

        private void Awake()
        {
            EnsureCapacity(_barCount);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<HudVisibilityChangedEvent>(OnHudVisibilityChanged);
        }

        private void OnHudVisibilityChanged(HudVisibilityChangedEvent evt) => Root.SetActive(evt.Visible);

        private void Update()
        {
            var percent = _health.MaxHealth > 0f ? _health.CurrentHealth / _health.MaxHealth : 0f;
            var activeBars = Mathf.RoundToInt(percent * _barCount);

            for (var i = 0; i < _bars.Count; i++)
            {
                _bars[i].SetActive(i < activeBars);
            }

            _healthText.text = $"{Mathf.CeilToInt(_health.CurrentHealth)} / {Mathf.CeilToInt(_health.MaxHealth)}";
        }

        private void EnsureCapacity(int count)
        {
            while (_bars.Count < count)
            {
                _bars.Add(Instantiate(_barPrefab, _barsContainer));
            }
        }
    }
}
