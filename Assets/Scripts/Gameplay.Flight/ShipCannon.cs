using Game.Core;
using Game.Data;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Flight
{
    public sealed class ShipCannon : MonoBehaviour, IWeapon
    {
        [SerializeField] private Transform _muzzle;
        [SerializeField] private WeaponConfig _weaponConfig;
        [SerializeField] private Animator _muzzleFlashAnimator;

        private static readonly int ShootHash = Animator.StringToHash("Shoot");

        private IShipInputProvider _shipInput;
        private float _cooldownRemaining;

        [Inject]
        public void Construct(IShipInputProvider shipInput)
        {
            _shipInput = shipInput;
        }

        private void Update()
        {
            _cooldownRemaining -= Time.deltaTime;

            if (_shipInput.FirePressed && _cooldownRemaining <= 0f)
            {
                Fire(_muzzle.position, ApplySpread(transform.right), gameObject);
            }
        }

        private Vector2 ApplySpread(Vector2 direction)
        {
            if (_weaponConfig.SpreadDegrees <= 0f)
            {
                return direction;
            }

            var offset = Random.Range(-_weaponConfig.SpreadDegrees, _weaponConfig.SpreadDegrees);
            return Quaternion.Euler(0f, 0f, offset) * direction;
        }

        public void Fire(Vector2 origin, Vector2 direction, GameObject source)
        {
            var rotation = Quaternion.FromToRotation(Vector3.right, direction);
            var projectile = Instantiate(_weaponConfig.ProjectilePrefab, origin, rotation);
            projectile.GetComponent<Projectile>().Launch(
                direction,
                _weaponConfig.ProjectileSpeed,
                _weaponConfig.Damage,
                _weaponConfig.DamageType,
                source,
                _weaponConfig.ProjectileLifetime);

            _cooldownRemaining = 1f / _weaponConfig.FireRate;

            _muzzleFlashAnimator.SetTrigger(ShootHash);
        }
    }
}
