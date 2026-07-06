using Game.Core;
using Game.Data;
using Game.Gameplay.Shared;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Flight
{
    [RequireComponent(typeof(Health))]
    public sealed class AiCombatInput : MonoBehaviour, IShipInputProvider
    {
        [SerializeField] private EnemyCombatConfig _config;

        private PlayerMarker _player;
        private EventBus _eventBus;
        private Health _health;
        private Rigidbody2D _rigidbody;
        private bool _alerted;
        private bool _alertPending;
        private float _alertDelayRemaining;

        public float Throttle { get; private set; }
        public float Rotation { get; private set; }
        public bool FirePressed { get; private set; }
        public bool InteractPressed => false;

        [Inject]
        public void Construct(PlayerMarker player, EventBus eventBus)
        {
            _player = player;
            _eventBus = eventBus;
        }

        private void Awake()
        {
            _health = GetComponent<Health>();
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _eventBus.Subscribe<EnemyAlertEvent>(OnEnemyAlert);
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            _eventBus.Unsubscribe<EnemyAlertEvent>(OnEnemyAlert);
        }

        private void Update()
        {
            TickAlertDelay();

            if (!_alerted && Vector2.Distance(transform.position, _player.Position) <= _config.DetectionRadius)
            {
                _alerted = true;
            }

            if (!_alerted)
            {
                Throttle = 0f;
                Rotation = 0f;
                FirePressed = false;
                return;
            }

            var toPlayer = _player.Position - (Vector2)transform.position;
            var distance = toPlayer.magnitude;

            Rotation = GetSteering(toPlayer);
            Throttle = GetThrottle(distance);

            var angle = Vector2.Angle(transform.right, toPlayer);
            FirePressed = angle <= _config.AimToleranceDegrees && distance <= _config.AttackRange;
        }

        private float GetSteering(Vector2 toPlayer)
        {
            var signedAngle = Vector2.SignedAngle(transform.right, toPlayer);
            var correction = signedAngle * _config.TurnResponsiveness - _rigidbody.angularVelocity * _config.TurnDamping;
            return Mathf.Clamp(correction / 90f, -1f, 1f);
        }

        private float GetThrottle(float distance)
        {
            switch (_config.MovementStyle)
            {
                case EnemyMovementStyle.Charge:
                    return 1f;

                case EnemyMovementStyle.Kite:
                    return distance < _config.KiteDistance ? -1f : 0f;

                default:
                    if (distance > _config.PreferredRange + _config.RangeTolerance)
                    {
                        return 1f;
                    }

                    if (distance < _config.PreferredRange - _config.RangeTolerance)
                    {
                        return -1f;
                    }

                    return 0f;
            }
        }

        private void OnDamaged(DamageInfo info)
        {
            _alerted = true;
            _eventBus.Publish(new EnemyAlertEvent(transform.position, _config.AlertRadius, _config.AlertPropagationDelay));
        }

        private void OnEnemyAlert(EnemyAlertEvent alert)
        {
            if (_alerted || _alertPending)
            {
                return;
            }

            if (Vector2.Distance(transform.position, alert.Position) <= alert.Radius)
            {
                _alertPending = true;
                _alertDelayRemaining = alert.Delay;
            }
        }

        private void TickAlertDelay()
        {
            if (!_alertPending)
            {
                return;
            }

            _alertDelayRemaining -= Time.deltaTime;
            if (_alertDelayRemaining <= 0f)
            {
                _alertPending = false;
                _alerted = true;
            }
        }
    }
}
