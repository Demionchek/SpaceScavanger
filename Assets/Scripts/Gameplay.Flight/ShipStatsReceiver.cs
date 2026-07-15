using Game.Core;
using Game.Gameplay.Shared;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Flight
{
    // Применяет апгрейды из IShipStatsService к компонентам корабля ИГРОКА.
    // Контроллеры общие с врагами, поэтому статы прокидываются только сюда,
    // а не инъектируются в сами контроллеры.
    public sealed class ShipStatsReceiver : MonoBehaviour
    {
        [SerializeField] private ShipMovementController _movement;
        [SerializeField] private ShipCannon _cannon;
        [SerializeField] private HookController _hook;
        [SerializeField] private Health _health;

        private IShipStatsService _stats;
        private EventBus _eventBus;
        private bool _subscribed;

        [Inject]
        public void Construct(IShipStatsService stats, EventBus eventBus)
        {
            _stats = stats;
            _eventBus = eventBus;

            // Инъекция может произойти позже OnEnable — подписываемся из обеих точек.
            if (isActiveAndEnabled)
            {
                Subscribe();
                Apply();
            }
        }

        private void OnEnable()
        {
            if (_eventBus != null)
            {
                Subscribe();
                Apply();
            }
        }

        private void OnDisable()
        {
            if (_subscribed)
            {
                _eventBus.Unsubscribe<ShipStatsChangedEvent>(OnStatsChanged);
                _subscribed = false;
            }
        }

        private void Subscribe()
        {
            if (_subscribed)
            {
                return;
            }

            _eventBus.Subscribe<ShipStatsChangedEvent>(OnStatsChanged);
            _subscribed = true;
        }

        private void OnStatsChanged(ShipStatsChangedEvent evt)
        {
            Apply();
        }

        private void Apply()
        {
            if (_movement != null)
            {
                _movement.SetThrustMultiplier(_stats.GetMultiplier(ShipStat.ThrustForce));
            }

            if (_cannon != null)
            {
                _cannon.SetCombatMultipliers(
                    _stats.GetMultiplier(ShipStat.Damage),
                    _stats.GetMultiplier(ShipStat.FireRate));
            }

            if (_hook != null)
            {
                _hook.SetHookLevelBonus(_stats.GetBonus(ShipStat.HookLevel));
            }

            if (_health != null)
            {
                _health.SetMaxHealthMultiplier(_stats.GetMultiplier(ShipStat.MaxHealth));
            }
        }
    }
}
