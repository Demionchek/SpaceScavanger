using UnityEngine;

namespace Game.Core
{
    // Позиция игрока для систем, которым нужен только локатор (например,
    // HUD-радар), без ссылки на конкретный PlayerMarker из Gameplay.Flight.
    public interface IPlayerLocator
    {
        Vector2 Position { get; }
    }
}
