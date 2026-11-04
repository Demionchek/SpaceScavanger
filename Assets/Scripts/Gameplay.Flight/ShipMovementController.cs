using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Flight
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class ShipMovementController : MonoBehaviour
    {
        [SerializeField] private float _thrustForce = 10f;

        [Tooltip("Как быстро гаснет скорость, когда тяга отпущена. 0 — инерция как в вакууме.")]
        [SerializeField] private float _passiveDeceleration = 6f;

        [Tooltip("Потолок скорости. 0 — без потолка.")]
        [SerializeField] private float _maxSpeed = 12f;
        [SerializeField] private float _brakeDeceleration = 5f;

        [Tooltip("Задний ход. У игрока выключен — «назад» только тормозит; ИИ он нужен, " +
                 "чтобы отходить от цели.")]
        [SerializeField] private bool _allowReverse = true;

        [SerializeField] private float _reverseThrustForce = 5f;
        [SerializeField] private float _stopThreshold = 0.05f;
        [SerializeField] private float _angularStopThreshold = 1f;
        [SerializeField] private float _torqueStrength = 90f;
        [SerializeField] private float _maxAngularSpeed = 180f;
        [SerializeField] private float _angularDamping = 90f;
        [SerializeField] private float _brakeAngularDamping = 240f;

        private Rigidbody2D _rigidbody;
        private IShipInputProvider _shipInput;
        private float _thrustMultiplier = 1f;
        private bool _reversing;

        public float CurrentThrottle => _shipInput.Throttle;

        private bool IsStopped => _rigidbody.linearVelocity.magnitude <= _stopThreshold &&
                                  Mathf.Abs(_rigidbody.angularVelocity) <= _angularStopThreshold;

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
            var braking = false;

            if (throttle > 0f)
            {
                _reversing = false;
                _rigidbody.AddForce(transform.right * (throttle * _thrustForce * _thrustMultiplier));
            }
            else if (throttle < 0f)
            {
                // Тормоз гасит и ход, и вращение. Задний ход включается только после
                // полной остановки и только там, где разрешён: у игрока его нет,
                // а ИИ им отходит от цели.
                if (!_allowReverse || (!_reversing && !IsStopped))
                {
                    braking = true;
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

                // Без тяги корабль сам сбрасывает ход: управлять так проще, чем
                // вечной инерцией, и не нужно разворачиваться, чтобы затормозить.
                if (_passiveDeceleration > 0f)
                {
                    _rigidbody.linearVelocity = Vector2.MoveTowards(
                        _rigidbody.linearVelocity,
                        Vector2.zero,
                        _passiveDeceleration * Time.fixedDeltaTime);
                }
            }

            var rotation = _shipInput.Rotation;

            if (braking)
            {
                // Тормоз гасит и вращение, причём сильнее обычного демпфирования
                // и в обход ввода поворота: это полная остановка корабля.
                _rigidbody.angularVelocity = Mathf.MoveTowards(
                    _rigidbody.angularVelocity,
                    0f,
                    _brakeAngularDamping * Time.fixedDeltaTime);
            }
            else if (Mathf.Approximately(rotation, 0f))
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

            if (_maxSpeed > 0f && _rigidbody.linearVelocity.sqrMagnitude > _maxSpeed * _maxSpeed)
            {
                _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * _maxSpeed;
            }
        }
    }
}
