using Game.Core;
using Game.Data;
using UnityEngine;
using UnityEngine.Audio;
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
        [Tooltip("Зерно генерации зоны. 0 — своё на каждый запуск; иначе сектора " +
                 "повторяются от игры к игре. Сейв хранит своё зерно и грузится как было.")]
        [SerializeField] private int _zoneSeed;

        [Tooltip("Эффект взрыва корабля игрока. Можно не задавать — тогда просто пауза перед поражением.")]
        [SerializeField] private GameObject _playerExplosionPrefab;

        [Tooltip("Сколько секунд взрыв играет до экрана поражения.")]
        [SerializeField] private float _playerDeathDelay = 1.5f;

        [Tooltip("Звуки взрыва корабля: один берётся случайно. У игрока играет в 2D, " +
                 "у врага — с его позиции.")]
        [SerializeField] private AudioClip[] _explosionClips;

        [SerializeField] private AudioMixerGroup _explosionMixerGroup;

        [Tooltip("Сколько секунд живёт эффект взрыва врага.")]
        [SerializeField] private float _explosionLifetime = 1.5f;

        [Tooltip("Во сколько раз взрыв врага мельче: эффект нарисован под корабль игрока.")]
        [SerializeField] private float _enemyExplosionScale = 0.8f;

        [Tooltip("Группа микшера музыки космоса: её источники глохнут в интерьере, " +
                 "где играет своя музыка.")]
        [SerializeField] private AudioMixerGroup _spaceMusicGroup;

        [Tooltip("Снять для обучающего уровня: коридор собран в сцене вручную, " +
                 "случайная зона появится после первого прыжка червоточиной.")]
        [SerializeField] private bool _generateZoneOnStart = true;

        [Header("Туман на краю зоны")]
        [Tooltip("Цвет стены тумана. Альфа берётся из _fogMaxAlpha, а не отсюда.")]
        [SerializeField] private Color _fogColor = new(0.05f, 0.07f, 0.12f);

        [Tooltip("Плотность у границы. 0 — тумана нет совсем.")]
        [Range(0f, 1f)] [SerializeField] private float _fogMaxAlpha = 0.95f;

        [Tooltip("Какая доля этой плотности набирается к дальнему углу зоны. Там ещё " +
                 "лежит добро, поэтому дымка должна оставаться прозрачной.")]
        [Range(0f, 1f)] [SerializeField] private float _fogHazeFraction = 0.25f;

        [Tooltip("Слой отрисовки: выше содержимого зоны (5) и ниже кораблей (19).")]
        [SerializeField] private int _fogSortingOrder = 15;

        [Tooltip("Где туман начинается, в единицах от центра зоны. 0 — считать от ZoneBounds.")]
        [SerializeField] private float _fogInnerRadius = 38f;

        [Tooltip("Где он набирает полную плотность. 0 — у самой черты гибели " +
                 "(SafeRadius + DeathDistance).")]
        [SerializeField] private float _fogOuterRadius;

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
            builder.RegisterInstance(new ExplosionKit(_playerExplosionPrefab, _explosionClips,
                _explosionMixerGroup, _explosionLifetime, _enemyExplosionScale));
            // Ноль — зерно на запуск: иначе первый прыжок всегда приводил в один и
            // тот же сектор, потому что ZoneSpawner просто увеличивает его на единицу.
            builder.RegisterInstance(new ZoneSeed(
                _zoneSeed != 0 ? _zoneSeed : Random.Range(int.MinValue, int.MaxValue)));
            builder.Register<RandomZoneGenerator>(Lifetime.Singleton).As<IZoneGenerator>();
            builder.RegisterComponentInHierarchy<WormholeTravelController>();
            builder.RegisterEntryPoint<PlayerDeathController>(Lifetime.Singleton)
                .WithParameter("explosionPrefab", _playerExplosionPrefab)
                .WithParameter("deathDelay", _playerDeathDelay);
            builder.RegisterEntryPoint<BoundaryFog>(Lifetime.Singleton)
                .WithParameter("fogColor", _fogColor)
                .WithParameter("maxAlpha", _fogMaxAlpha)
                .WithParameter("hazeFraction", _fogHazeFraction)
                .WithParameter("sortingOrder", _fogSortingOrder)
                .WithParameter("innerRadius", _fogInnerRadius)
                .WithParameter("outerRadius", _fogOuterRadius);
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

                foreach (var puzzle in Object.FindObjectsByType<SpacePuzzleComponent>(
                             FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    container.Inject(puzzle);
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
            builder.RegisterEntryPoint<SpaceMusicMute>(Lifetime.Singleton)
                .WithParameter("musicGroup", _spaceMusicGroup);
            builder.RegisterEntryPoint<BoardingTutorial>(Lifetime.Singleton);
            builder.RegisterEntryPoint<QuestEnemySpawner>(Lifetime.Singleton);
            builder.RegisterEntryPoint<RaceManager>(Lifetime.Singleton);
        }
    }
}
