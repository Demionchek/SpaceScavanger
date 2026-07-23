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
        private bool _engaged;
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
                SetAlerted();
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
            Throttle = GetThrottle(distance, toPlayer);

            var angle = Vector2.Angle(transform.right, toPlayer);
            FirePressed = angle <= _config.AimToleranceDegrees && distance <= _config.AttackRange;
        }

        private float GetSteering(Vector2 toPlayer)
        {
            var signedAngle = Vector2.SignedAngle(transform.right, toPlayer);
            return ShipPilot.Steer(_rigidbody, signedAngle, _config.TurnResponsiveness, _config.TurnDamping);
        }

        private float GetThrottle(float distance, Vector2 toPlayer)
        {
            switch (_config.MovementStyle)
            {
                case EnemyMovementStyle.Charge:
                    // Даже в лобовой атаке гасим пролёт, иначе кружит по орбите.
                    return ShipPilot.DriftingPastTarget(_rigidbody, toPlayer, _config.OrbitBrakeSpeed, _config.OrbitAlignment)
                        ? -1f
                        : 1f;

                case EnemyMovementStyle.Kite:
                    return distance < _config.KiteDistance ? -1f : 0f;

                default:
                    if (distance > _config.PreferredRange + _config.RangeTolerance)
                    {
                        // Приближаемся к дистанции удержания, но летим слишком быстро
                        // (в лоб) или проскакиваем вбок (орбита) — тормозим, как игрок.
                        if ((distance - _config.PreferredRange < _config.BrakeMargin
                                && ShipPilot.ClosingSpeed(_rigidbody, toPlayer, distance) > _config.BrakeClosingSpeed)
                            || ShipPilot.DriftingPastTarget(_rigidbody, toPlayer, _config.OrbitBrakeSpeed, _config.OrbitAlignment))
                        {
                            return -1f;
                        }

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
            SetAlerted();
            _eventBus.Publish(new EnemyAlertEvent(transform.position, _config.AlertRadius, _config.AlertPropagationDelay));
        }

        // Первый переход в тревогу = вступление в бой: сообщаем трекеру, чтобы
        // держал ModeLock (нельзя уйти в интерьер, пока враг в бою).
        private void SetAlerted()
        {
            if (_alerted)
            {
                return;
            }

            _alerted = true;
            _engaged = true;
            _eventBus.Publish(new CombatEngagementEvent(true));
        }

        private void OnDestroy()
        {
            if (_engaged)
            {
                _engaged = false;
                _eventBus?.Publish(new CombatEngagementEvent(false));
            }
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
                SetAlerted();
            }
        }
    }
}
