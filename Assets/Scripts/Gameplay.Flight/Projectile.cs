using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Flight
{
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _rigidbody;

        private float _damage;
        private DamageType _damageType;
        private GameObject _source;

        public void Launch(Vector2 direction, float speed, float damage, DamageType damageType, GameObject source, float lifetime)
        {
            _damage = damage;
            _damageType = damageType;
            _source = source;
            _rigidbody.linearVelocity = direction.normalized * speed;
            Destroy(gameObject, lifetime);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject == _source)
            {
                return;
            }

            if (!other.TryGetComponent<IDamageable>(out var damageable))
            {
                return;
            }

            damageable.TakeDamage(new DamageInfo { Amount = _damage, Type = _damageType, Source = _source });
            Destroy(gameObject);
        }
    }
}
