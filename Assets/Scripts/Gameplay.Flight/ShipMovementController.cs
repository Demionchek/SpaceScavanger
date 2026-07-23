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
        [SerializeField] private float _reverseThrustForce = 5f;
        [SerializeField] private float _stopThreshold = 0.05f;
        [SerializeField] private float _torqueStrength = 90f;
        [SerializeField] private float _maxAngularSpeed = 180f;
        [SerializeField] private float _angularDamping = 90f;

        private Rigidbody2D _rigidbody;
        private IShipInputProvider _shipInput;
        private float _thrustMultiplier = 1f;
        private bool _reversing;

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
                _reversing = false;
                _rigidbody.AddForce(transform.right * (throttle * _thrustForce * _thrustMultiplier));
            }
            else if (throttle < 0f)
            {
                // Сначала полностью гасим скорость в любом направлении. Флаг _reversing
                // фиксирует переход в задний ход, чтобы набранная реверсом скорость
                // не попадала обратно под торможение.
                if (!_reversing && _rigidbody.linearVelocity.magnitude > _stopThreshold)
                {
                    _rigidbody.linearVelocity = Vector2.MoveTowards(
                        _rigidbody.linearVelocity,
                        Vector2.zero,
                        -throttle * _brakeDeceleration * Time.fixedDeltaTime);
                }
                else
                {
                    _reversing = true;
                    _rigidbody.AddForce(-transform.right * (-throttle * _reverseThrustForce * _thrustMultiplier));
                }
            }
            else
            {
                _reversing = false;
            }

            var rotation = _shipInput.Rotation;

            if (Mathf.Approximately(rotation, 0f))
            {
                // Нет ввода поворота — плавно гасим вращение до остановки.
                _rigidbody.angularVelocity = Mathf.MoveTowards(
                    _rigidbody.angularVelocity,
                    0f,
                    _angularDamping * Time.fixedDeltaTime);
            }
            else
            {
                _rigidbody.AddTorque(rotation * _torqueStrength);
            }

            _rigidbody.angularVelocity = Mathf.Clamp(_rigidbody.angularVelocity, -_maxAngularSpeed, _maxAngularSpeed);
        }
    }
}
