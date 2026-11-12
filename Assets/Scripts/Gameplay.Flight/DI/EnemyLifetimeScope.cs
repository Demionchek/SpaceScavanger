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

        [Tooltip("Взрыв при гибели. Scope инъектит только то, что здесь зарегистрировано, " +
                 "autoInjectGameObjects у префаба пуст.")]
        [SerializeField] private ShipExplosion _explosion;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent<IShipInputProvider>(_combatInput);
            builder.RegisterComponent(_movement);
            builder.RegisterComponent(_cannon);

            if (_groupTag != null)
            {
                builder.RegisterComponent(_groupTag);
            }

            if (_explosion != null)
            {
                builder.RegisterComponent(_explosion);
            }
        }
    }
}
