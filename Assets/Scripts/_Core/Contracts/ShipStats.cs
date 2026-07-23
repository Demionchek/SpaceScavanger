using System;

namespace Game.Core
{
    public enum ShipStat
    {
        ThrustForce,
        Damage,
        FireRate,
        HookLevel,
        MaxHealth,
        RadarRange
    }

    [Serializable]
    public struct ShipStatModifier
    {
        public ShipStat Stat;
        public float Multiplier;
        public int FlatBonus;
    }

    public readonly struct ShipStatsChangedEvent
    {
    }

    public interface IShipStatsService
    {
        float GetMultiplier(ShipStat stat);
        int GetBonus(ShipStat stat);
        void ApplyModifiers(ShipStatModifier[] modifiers);
    }
}
