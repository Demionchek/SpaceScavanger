using System.Collections.Generic;
using Game.Core;

namespace Game.Gameplay.Shared
{
    public sealed class ShipStatsService : IShipStatsService
    {
        private readonly EventBus _eventBus;
        private readonly Dictionary<ShipStat, float> _multipliers = new();
        private readonly Dictionary<ShipStat, int> _bonuses = new();

        public ShipStatsService(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public float GetMultiplier(ShipStat stat)
        {
            return _multipliers.TryGetValue(stat, out var value) ? value : 1f;
        }

        public int GetBonus(ShipStat stat)
        {
            return _bonuses.GetValueOrDefault(stat);
        }

        public void ApplyModifiers(ShipStatModifier[] modifiers)
        {
            if (modifiers == null || modifiers.Length == 0)
            {
                return;
            }

            foreach (var modifier in modifiers)
            {
                if (modifier.Multiplier > 0f)
                {
                    _multipliers[modifier.Stat] = GetMultiplier(modifier.Stat) * modifier.Multiplier;
                }

                if (modifier.FlatBonus != 0)
                {
                    _bonuses[modifier.Stat] = GetBonus(modifier.Stat) + modifier.FlatBonus;
                }
            }

            _eventBus.Publish(new ShipStatsChangedEvent());
        }
    }
}
