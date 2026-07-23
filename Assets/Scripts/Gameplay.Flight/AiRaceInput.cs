using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Flight
{
    public sealed class AiRaceInput : MonoBehaviour, IShipInputProvider
    {
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private LayerMask _obstacleMask;
        [SerializeField] private float _sensorLength = 5f;
        [SerializeField] private float _sensorAngle = 30f;
        [SerializeField] private float _avoidStrength = 0.6f;
        [SerializeField] private float _turnResponsiveness = 1.5f;
        [SerializeField] private float _turnDamping = 0.5f;
        [SerializeField] private float _velocityLead = 0.5f;
        [SerializeField] private float _maxSpeed = 6f;
        [SerializeField] private float _fullThrottleAngle = 45f;
        [SerializeField] private float _turnThrottle = 0.25f;
        [SerializeField] private float _slowRadius = 5f;
        [SerializeField] private float _brakeClosingSpeed = 3f;
        [SerializeField] private float _orbitBrakeSpeed = 3f;
        [SerializeField] private float _orbitAlignment = 0.5f;

        private Vector2 _target;
        private bool _hasTarget;
        private float _throttle;
        private float _rotation;

        public float Throttle => _throttle;
        public float Rotation => _rotation;
        public bool FirePressed => false;
        public bool InteractPressed => false;

        public void SetTarget(Vector2 target)
        {
            _target = target;
            _hasTarget = true;
        }

        public void ClearTarget()
        {
            _hasTarget = false;
        }

        private void Update()
        {
            if (!_hasTarget)
            {
                _throttle = 0f;
                _rotation = 0f;
                return;
            }

            Vector2 position = transform.position;
            var toTarget = _target - position;
            var distance = toTarget.magnitude;

            // Упреждение инерции, но не больше половины дистанции до цели,
            // иначе на высокой скорости AI целится «в никуда» и рыскает.
            var lead = Vector2.ClampMagnitude(_rigidbody.linearVelocity * _velocityLead, distance * 0.5f);
            var desired = toTarget - lead;

            var signedAngle = Vector2.SignedAngle(transform.right, desired);
            var steering = ShipPilot.Steer(_rigidbody, signedAngle, _turnResponsiveness, _turnDamping);
            steering += ShipPilot.ObstacleAvoidance(transform, _obstacleMask, _sensorLength, _sensorAngle) * _avoidStrength;
            _rotation = Mathf.Clamp(steering, -1f, 1f);

            _throttle = ComputeThrottle(signedAngle, distance, toTarget);
        }

        private float ComputeThrottle(float angleToTarget, float distance, Vector2 toTarget)
        {
            var speed = _rigidbody.linearVelocity.magnitude;

            // Пролёт/орбита: летим быстро мимо точки — тормозим, чтобы сбросить
            // боковую скорость и суметь довернуть, а не наматывать круги.
            if (ShipPilot.DriftingPastTarget(_rigidbody, toTarget, _orbitBrakeSpeed, _orbitAlignment))
            {
                return -1f;
            }

            // Понимаем, что перелетаем: близко к цели и подходим слишком быстро —
            // тормозим тем же механизмом, что и игрок (отрицательный throttle).
            if (distance < _slowRadius && ShipPilot.ClosingSpeed(_rigidbody, toTarget, distance) > _brakeClosingSpeed)
            {
                return -1f;
            }

            if (speed > _maxSpeed)
            {
                return -0.5f;
            }

            if (Mathf.Abs(angleToTarget) > _fullThrottleAngle)
            {
                return _turnThrottle;
            }

            return 1f;
        }
    }
}
