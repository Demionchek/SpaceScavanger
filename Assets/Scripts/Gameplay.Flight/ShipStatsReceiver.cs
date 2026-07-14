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

        [Inject]
        public void Construct(IShipStatsService stats, EventBus eventBus)
        {
            _stats = stats;
            _eventBus = eventBus;
        }

        private void OnEnable()
        {
            _eventBus.Subscribe<ShipStatsChangedEvent>(OnStatsChanged);
            Apply();
        }

        private void OnDisable()
        {
            _eventBus.Unsubscribe<ShipStatsChangedEvent>(OnStatsChanged);
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
