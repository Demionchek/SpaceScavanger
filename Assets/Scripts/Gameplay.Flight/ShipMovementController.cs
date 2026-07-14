using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Flight
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class ShipMovementController : MonoBehaviour
    {
        [SerializeField] private float _thrustForce = 10f;
        [SerializeField] private float _brakeDeceleration = 5f;
        [SerializeField] private float _torqueStrength = 90f;
        [SerializeField] private float _maxAngularSpeed = 180f;

        private Rigidbody2D _rigidbody;
        private IShipInputProvider _shipInput;
        private float _thrustMultiplier = 1f;

        public float CurrentThrottle => _shipInput.Throttle;

        public void SetThrustMultiplier(float multiplier)
        {
            _thrustMultiplier = multiplier;
        }

        [Inject]
        public void Construct(IShipInputProvider shipInput)
        {
            _shipInput = shipInput;
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.gravityScale = 0f;
        }

        // Assumes the ship sprite faces +X (transform.right = nose/forward).
        private void FixedUpdate()
        {
            var throttle = _shipInput.Throttle;

            if (throttle > 0f)
            {
                _rigidbody.AddForce(transform.right * (throttle * _thrustForce * _thrustMultiplier));
            }
            else if (throttle < 0f)
            {
                _rigidbody.linearVelocity = Vector2.MoveTowards(
                    _rigidbody.linearVelocity,
                    Vector2.zero,
                    -throttle * _brakeDeceleration * Time.fixedDeltaTime);
            }

            _rigidbody.AddTorque(_shipInput.Rotation * _torqueStrength);
            _rigidbody.angularVelocity = Mathf.Clamp(_rigidbody.angularVelocity, -_maxAngularSpeed, _maxAngularSpeed);
        }
    }
}
