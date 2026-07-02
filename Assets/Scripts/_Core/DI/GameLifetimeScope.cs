using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace Game.Core
{
    public sealed class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private List<ScriptableObjectInstaller> _featureInstallers = new();

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_inputActions);
            builder.Register<EventBus>(Lifetime.Singleton);
            builder.Register<PlayerContext>(Lifetime.Singleton);

            builder.Register<ShipInteriorState>(Lifetime.Singleton).As<IGameState>();
            builder.Register<SpaceFlightState>(Lifetime.Singleton).As<IGameState>();
            builder.Register<BoardingState>(Lifetime.Singleton).As<IGameState>();
            builder.Register<GameStateMachine>(Lifetime.Singleton);

            // Registration order matters: InputMapSwitcher must subscribe before
            // GameStateMachineBootstrap fires the first state change below.
            builder.RegisterEntryPoint<InputMapSwitcher>(Lifetime.Singleton);
            builder.RegisterEntryPoint<GameStateMachineBootstrap>(Lifetime.Singleton);
            builder.RegisterEntryPoint<DebugStateHotkeys>(Lifetime.Singleton);
            builder.RegisterEntryPoint<DebugResourceLogger>(Lifetime.Singleton);

            foreach (var installer in _featureInstallers)
            {
                installer.Install(builder);
            }
        }
    }
}
