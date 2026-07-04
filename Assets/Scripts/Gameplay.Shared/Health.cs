using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private float _maxHealth = 100f;

        private float _currentHealth;

        private void Awake()
        {
            _currentHealth = _maxHealth;
        }

        public void TakeDamage(DamageInfo info)
        {
            _currentHealth -= info.Amount;

            if (_currentHealth <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
