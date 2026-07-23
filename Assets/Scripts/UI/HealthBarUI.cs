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
        public void Construct(Health health, EventBus eventBus)
        {
            _health = health;
            _eventBus = eventBus;

            // Подписка в Construct: инъекция может пройти позже OnEnable, а
            // стартовое событие SpaceFlightState публикуется bootstrap'ом сразу
            // после сборки контейнера — иначе пропустим его.
            _eventBus.Subscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void Awake()
        {
            EnsureCapacity(_barCount);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<GameStateChangedEvent>(OnGameStateChanged);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            // HUD здоровья корабля виден в полёте и скрыт внутри корабля.
            if (evt.NewState is ShipInteriorState)
            {
                Root.SetActive(false);
            }
            else if (evt.NewState is SpaceFlightState)
            {
                Root.SetActive(true);
            }
        }

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
