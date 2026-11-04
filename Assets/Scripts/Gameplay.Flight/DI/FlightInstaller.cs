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

        [Tooltip("Тиры по номеру сектора: враги, их число и количество ресурсов. " +
                 "Пусто — зона всегда генерируется по ZoneConfig.")]
        [SerializeField] private ZoneProgressionConfig _zoneProgression;
        [SerializeField] private int _zoneSeed = 12345;

        [Tooltip("Эффект взрыва корабля игрока. Можно не задавать — тогда просто пауза перед поражением.")]
        [SerializeField] private GameObject _playerExplosionPrefab;

        [Tooltip("Сколько секунд взрыв играет до экрана поражения.")]
        [SerializeField] private float _playerDeathDelay = 1.5f;

        [Tooltip("Снять для обучающего уровня: коридор собран в сцене вручную, " +
                 "случайная зона появится после первого прыжка червоточиной.")]
        [SerializeField] private bool _generateZoneOnStart = true;

        public override void Install(IContainerBuilder builder)
        {
            builder.Register<PlayerShipInput>(Lifetime.Singleton).As<IShipInputProvider>();
            builder.RegisterComponentInHierarchy<ShipMovementController>();
            builder.RegisterComponentInHierarchy<HookController>();
            builder.RegisterComponentInHierarchy<ShipCannon>();
            builder.RegisterComponentInHierarchy<PlayerMarker>().AsSelf().As<IPlayerLocator>();
            builder.RegisterComponentInHierarchy<ShipInteractor>();
            builder.RegisterComponentInHierarchy<EngineSoundController>();
            builder.RegisterComponentInHierarchy<ShipStatsReceiver>();

            builder.RegisterInstance(_zoneConfig);
            builder.RegisterInstance(new ZoneSeed(_zoneSeed));
            builder.Register<RandomZoneGenerator>(Lifetime.Singleton).As<IZoneGenerator>();
            builder.RegisterComponentInHierarchy<WormholeTravelController>();
            builder.RegisterEntryPoint<PlayerDeathController>(Lifetime.Singleton)
                .WithParameter("explosionPrefab", _playerExplosionPrefab)
                .WithParameter("deathDelay", _playerDeathDelay);
            builder.RegisterEntryPoint<ZoneSpawner>(Lifetime.Singleton)
                .WithParameter<ZoneProgressionConfig>(_zoneProgression)
                .WithParameter("generateOnStart", _generateZoneOnStart);

            // Объектов таких типов в сцене много, RegisterComponentInHierarchy взял бы
            // один. Заспавненные ZoneSpawner-ом получают инъекцию при Instantiate,
            // а расставленные вручную (обучающий уровень) — только здесь.
            builder.RegisterBuildCallback(container =>
            {
                foreach (var trigger in Object.FindObjectsByType<TutorialTrigger>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    container.Inject(trigger);
                }

                foreach (var resource in Object.FindObjectsByType<HookableResource>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    container.Inject(resource);
                }

                foreach (var grant in Object.FindObjectsByType<QuestGrantComponent>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    container.Inject(grant);
                }

                foreach (var wormhole in Object.FindObjectsByType<WormholeComponent>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    container.Inject(wormhole);
                }
            });

            var tutorialGoal = Object.FindFirstObjectByType<TutorialResourceGoal>(FindObjectsInactive.Include);
            if (tutorialGoal != null)
            {
                builder.RegisterComponent(tutorialGoal);
            }

            var planets = Object.FindFirstObjectByType<PlanetFieldBackground>(FindObjectsInactive.Include);
            if (planets != null)
            {
                builder.RegisterComponent(planets);
            }

            var callSound = Object.FindFirstObjectByType<IntercomCallSound>(FindObjectsInactive.Include);
            if (callSound != null)
            {
                builder.RegisterComponent(callSound);
            }

            var playerSaver = Object.FindFirstObjectByType<PlayerTransformSaver>(FindObjectsInactive.Include);
            if (playerSaver != null)
            {
                builder.RegisterInstance(playerSaver).As<ISaveable>();
            }
            builder.RegisterEntryPoint<BoardingTutorial>(Lifetime.Singleton);
            builder.RegisterEntryPoint<QuestEnemySpawner>(Lifetime.Singleton);
            builder.RegisterEntryPoint<RaceManager>(Lifetime.Singleton);
        }
    }
}
