using System;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _maxHealth = 100f;

        private float _currentHealth;
        private float _maxHealthMultiplier = 1f;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth * _maxHealthMultiplier;

        public void SetMaxHealthMultiplier(float multiplier)
        {
            var oldMax = MaxHealth;
            _maxHealthMultiplier = multiplier;
            var gained = MaxHealth - oldMax;

            if (gained > 0f)
            {
                _currentHealth += gained;
            }

            _currentHealth = Mathf.Min(_currentHealth, MaxHealth);
        }

        public event Action<DamageInfo> Damaged;
        public event Action Died;

        private void Awake()
        {
            _currentHealth = _maxHealth;
        }

        public void TakeDamage(DamageInfo info)
        {
            _currentHealth -= info.Amount;
            Damaged?.Invoke(info);

            if (_currentHealth <= 0f)
            {
                Died?.Invoke();
                Destroy(gameObject);
            }
        }
    }
}
