using Game.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Flight
{
    public sealed class EnemyLifetimeScope : LifetimeScope
    {
        [SerializeField] private AiCombatInput _combatInput;
        [SerializeField] private ShipMovementController _movement;
        [SerializeField] private ShipCannon _cannon;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent<IShipInputProvider>(_combatInput);
            builder.RegisterComponent(_movement);
            builder.RegisterComponent(_cannon);
        }
    }
}
