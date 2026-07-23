using Game.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Flight
{
    // Имя класса намеренно НЕ заканчивается на "LifetimeScope":
    // VContainer затирает новые файлы *LifetimeScope.cs шаблоном при импорте.
    public sealed class RaceAiScope : LifetimeScope
    {
        [SerializeField] private AiRaceInput _raceInput;
        [SerializeField] private ShipMovementController _movement;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent<IShipInputProvider>(_raceInput);
            builder.RegisterComponent(_movement);
        }
    }
}
