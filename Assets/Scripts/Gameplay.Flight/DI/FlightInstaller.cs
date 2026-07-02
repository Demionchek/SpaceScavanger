using Game.Core;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Flight
{
    [CreateAssetMenu(menuName = "Game/Installers/Flight Installer", fileName = "FlightInstaller")]
    public sealed class FlightInstaller : ScriptableObjectInstaller
    {
        public override void Install(IContainerBuilder builder)
        {
            builder.Register<PlayerShipInput>(Lifetime.Singleton).As<IShipInputProvider>();
            builder.RegisterComponentInHierarchy<ShipMovementController>();
        }
    }
}
