using UnityEngine;

namespace Game.Data
{
    public enum EnemyMovementStyle
    {
        HoldRange,
        Charge,
        Kite
    }

    [CreateAssetMenu(menuName = "Game/Enemy/Enemy Combat Config", fileName = "EnemyCombatConfig")]
    public sealed class EnemyCombatConfig : ScriptableObject
    {
        public float DetectionRadius = 10f;
        public float AlertRadius = 6f;
        public float AlertPropagationDelay = 0.5f;

        public EnemyMovementStyle MovementStyle = EnemyMovementStyle.HoldRange;
        public float PreferredRange = 5f;
        public float RangeTolerance = 1f;
        public float KiteDistance = 8f;
        public float BrakeMargin = 3f;
        public float BrakeClosingSpeed = 3f;
        public float OrbitBrakeSpeed = 3f;
        public float OrbitAlignment = 0.5f;

        public float AimToleranceDegrees = 10f;
        public float AttackRange = 8f;
        public float TurnResponsiveness = 2f;
        public float TurnDamping = 0.5f;
    }
}
