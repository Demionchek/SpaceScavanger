using System;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _maxHealth = 100f;

        private float _currentHealth;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;

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
