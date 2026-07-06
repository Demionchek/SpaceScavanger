using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Weapons/Weapon Config", fileName = "WeaponConfig")]
    public sealed class WeaponConfig : ScriptableObject
    {
        public float Damage = 10f;
        public float FireRate = 4f;
        public float ProjectileSpeed = 20f;
        public float ProjectileLifetime = 2f;
        public float SpreadDegrees;
        public DamageType DamageType = DamageType.Kinetic;
        public GameObject ProjectilePrefab;
    }
}
