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

        // Базовое число стата знает только его владелец (пушка, двигатель, радар),
        // поэтому он сам сообщает его сюда — иначе экран состояния печатал бы
        // одни множители, которые игроку ничего не говорят.
        void SetBase(ShipStat stat, float value);

        // false — базу никто не зарегистрировал, показывать нечего.
        bool TryGetEffective(ShipStat stat, out float value);
    }
}
