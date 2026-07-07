using Game.Core;
using Game.Data;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Gameplay.Flight
{
    [CreateAssetMenu(menuName = "Game/Installers/Flight Installer", fileName = "FlightInstaller")]
    public sealed class FlightInstaller : ScriptableObjectInstaller
    {
        [SerializeField] private ZoneConfig _zoneConfig;
        [SerializeField] private int _zoneSeed = 12345;

        public override void Install(IContainerBuilder builder)
        {
            builder.Register<PlayerShipInput>(Lifetime.Singleton).As<IShipInputProvider>();
            builder.RegisterComponentInHierarchy<ShipMovementController>();
            builder.RegisterComponentInHierarchy<HookController>();
            builder.RegisterComponentInHierarchy<ShipCannon>();
            builder.RegisterComponentInHierarchy<PlayerMarker>();
            builder.RegisterComponentInHierarchy<ShipInteractor>();
            builder.RegisterComponentInHierarchy<EngineSoundController>();

            builder.RegisterInstance(_zoneConfig);
            builder.RegisterInstance(new ZoneSeed(_zoneSeed));
            builder.Register<RandomZoneGenerator>(Lifetime.Singleton).As<IZoneGenerator>();
            builder.RegisterEntryPoint<ZoneSpawner>(Lifetime.Singleton);
        }
    }
}
