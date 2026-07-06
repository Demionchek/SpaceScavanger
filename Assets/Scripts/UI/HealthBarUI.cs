using System.Collections.Generic;
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

        private readonly List<GameObject> _bars = new();
        private Health _health;

        [Inject]
        public void Construct(Health health)
        {
            _health = health;
        }

        private void Awake()
        {
            EnsureCapacity(_barCount);
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
