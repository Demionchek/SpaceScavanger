using Game.Core;
using Game.Gameplay.Shared;
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
        [SerializeField] private EnemyGroupTag _groupTag;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent<IShipInputProvider>(_combatInput);
            builder.RegisterComponent(_movement);
            builder.RegisterComponent(_cannon);

            if (_groupTag != null)
            {
                builder.RegisterComponent(_groupTag);
            }
        }
    }
}
