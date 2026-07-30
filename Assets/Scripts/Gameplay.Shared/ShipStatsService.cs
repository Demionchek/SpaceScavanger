using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    public sealed class ShipStatsService : IShipStatsService, ISaveable
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

                UnityEngine.Debug.Log(
                    $"Ship stat {modifier.Stat}: x{GetMultiplier(modifier.Stat)} +{GetBonus(modifier.Stat)}");
            }

            _eventBus.Publish(new ShipStatsChangedEvent());
        }

        public string SaveId => "shipstats";

        public string Save()
        {
            var data = new SaveData();
            foreach (var pair in _multipliers)
            {
                data.multStats.Add(pair.Key.ToString());
                data.multipliers.Add(pair.Value);
            }

            foreach (var pair in _bonuses)
            {
                data.bonusStats.Add(pair.Key.ToString());
                data.bonuses.Add(pair.Value);
            }

            return JsonUtility.ToJson(data);
        }

        public void Load(string json)
        {
            _multipliers.Clear();
            _bonuses.Clear();

            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                return;
            }

            for (var i = 0; i < data.multStats.Count; i++)
            {
                if (Enum.TryParse<ShipStat>(data.multStats[i], out var stat))
                {
                    _multipliers[stat] = data.multipliers[i];
                }
            }

            for (var i = 0; i < data.bonusStats.Count; i++)
            {
                if (Enum.TryParse<ShipStat>(data.bonusStats[i], out var stat))
                {
                    _bonuses[stat] = data.bonuses[i];
                }
            }

            _eventBus.Publish(new ShipStatsChangedEvent());
        }

        [Serializable]
        private sealed class SaveData
        {
            public List<string> multStats = new();
            public List<float> multipliers = new();
            public List<string> bonusStats = new();
            public List<int> bonuses = new();
        }
    }
}
