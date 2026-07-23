using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Game.Gameplay.Flight
{
    public sealed class RaceManager : IStartable, ITickable, IDisposable
    {
        private sealed class Racer
        {
            public Transform Ship;
            public AiRaceInput Ai;
            public int NextCheckpoint;
            public bool Finished;
            public bool IsPlayer;
        }

        private readonly EventBus _eventBus;
        private readonly LifetimeScope _rootScope;
        private readonly PlayerMarker _player;
        private readonly GameContext _gameContext;
        private readonly IPauseService _pauseService;

        private readonly List<Racer> _racers = new();
        private readonly List<Vector2> _checkpoints = new();
        private readonly List<GameObject> _spawned = new();
        private readonly List<CheckpointMarker> _markers = new();

        private RaceDefinition _race;
        private RaceWaypointIndicator _waypointIndicator;
        private bool _active;
        private bool _pendingStart;
        private bool _countingDown;
        private float _countdownRemaining;
        private int _lastAnnouncedSecond;
        private int _finishedCount;

        public RaceManager(
            EventBus eventBus,
            LifetimeScope rootScope,
            PlayerMarker player,
            GameContext gameContext,
            IPauseService pauseService)
        {
            _eventBus = eventBus;
            _rootScope = rootScope;
            _player = player;
            _gameContext = gameContext;
            _pauseService = pauseService;
        }

        public void Start()
        {
            _eventBus.Subscribe<RaceRequestedEvent>(OnRaceRequested);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<RaceRequestedEvent>(OnRaceRequested);
        }

        private void OnRaceRequested(RaceRequestedEvent evt)
        {
            if (_active || evt.Race == null || _player == null)
            {
                return;
            }

            _race = evt.Race;
            GenerateTrack(_player.transform.position);
            PlaceRacersOnStartLine();

            _waypointIndicator = _player.GetComponentInChildren<RaceWaypointIndicator>(true);
            _waypointIndicator?.SetTarget(_checkpoints[0]);

            _finishedCount = 0;

            // Эффект срабатывает изнутри диалога, мир уже на паузе. Ждём закрытия
            // диалога и только потом запускаем отсчёт со своей паузой.
            _pendingStart = true;
        }

        private void BeginCountdown()
        {
            _pendingStart = false;
            _countingDown = true;
            _countdownRemaining = Mathf.Max(1, _race.CountdownSeconds);
            _lastAnnouncedSecond = int.MaxValue;
            _pauseService.RequestPause();
        }

        private void PlaceRacersOnStartLine()
        {
            SpawnRacers();

            var startPoint = (Vector2)_player.transform.position;
            var toFirst = (_checkpoints[0] - startPoint).normalized;
            var startLine = new Vector2(-toFirst.y, toFirst.x);
            var facing = Quaternion.FromToRotation(Vector3.right, toFirst);

            for (var i = 0; i < _racers.Count; i++)
            {
                // Слоты вдоль стартовой линии: 0, +1, -1, +2, -2...
                var slot = (i + 1) / 2 * ((i % 2 == 0) ? 1 : -1);
                var position = startPoint + startLine * (slot * _race.StartLineSpacing);

                var racer = _racers[i];
                racer.Ship.SetPositionAndRotation(position, facing);

                var body = racer.Ship.GetComponentInChildren<Rigidbody2D>();
                if (body != null)
                {
                    body.linearVelocity = Vector2.zero;
                    body.angularVelocity = 0f;
                }
            }
        }

        private void BeginRacing()
        {
            _countingDown = false;
            _active = true;
            _pauseService.ReleasePause();
            _eventBus.Publish(new RaceCountdownEvent(0));
            _eventBus.Publish(new RaceStartedEvent(_checkpoints.Count));
        }

        private void GenerateTrack(Vector2 center)
        {
            _checkpoints.Clear();
            _markers.Clear();

            var startAngle = Random.Range(0f, 360f);
            for (var i = 0; i < _race.CheckpointCount; i++)
            {
                var angle = startAngle + 360f * i / _race.CheckpointCount;
                var radius = _race.TrackRadius * (1f + Random.Range(-_race.RadiusJitter, _race.RadiusJitter));
                var position = center + (Vector2)(Quaternion.Euler(0f, 0f, angle) * Vector3.right * radius);
                _checkpoints.Add(position);

                if (_race.CheckpointMarkerPrefab != null)
                {
                    var marker = Object.Instantiate(_race.CheckpointMarkerPrefab, position, Quaternion.identity);
                    _spawned.Add(marker);
                    _markers.Add(marker.GetComponent<CheckpointMarker>());
                }
            }
        }

        private void SpawnRacers()
        {
            _racers.Clear();
            _racers.Add(new Racer { Ship = _player.transform, IsPlayer = true });

            if (_race.AiRacers == null)
            {
                return;
            }

            foreach (var racerPrefab in _race.AiRacers)
            {
                if (racerPrefab == null)
                {
                    continue;
                }

                var prefabScope = racerPrefab.GetComponent<LifetimeScope>();
                var instanceScope = _rootScope.CreateChildFromPrefab(prefabScope);
                instanceScope.transform.SetParent(null);
                instanceScope.transform.position =
                    (Vector2)_player.transform.position + Random.insideUnitCircle.normalized * 3f;

                _spawned.Add(instanceScope.gameObject);

                var ai = instanceScope.GetComponentInChildren<AiRaceInput>();
                ai.SetTarget(_checkpoints[0]);
                _racers.Add(new Racer { Ship = instanceScope.transform, Ai = ai });
            }
        }

        public void Tick()
        {
            if (_pendingStart)
            {
                if (!_pauseService.IsPaused)
                {
                    BeginCountdown();
                }

                return;
            }

            if (_countingDown)
            {
                TickCountdown();
                return;
            }

            if (!_active)
            {
                return;
            }

            foreach (var racer in _racers)
            {
                if (racer.Finished || racer.Ship == null)
                {
                    continue;
                }

                var target = _checkpoints[racer.NextCheckpoint];
                if (Vector2.Distance(racer.Ship.position, target) > _race.CheckpointPassRadius)
                {
                    continue;
                }

                if (racer.IsPlayer && racer.NextCheckpoint < _markers.Count && _markers[racer.NextCheckpoint] != null)
                {
                    _markers[racer.NextCheckpoint].SetPassed();
                }

                racer.NextCheckpoint++;

                if (racer.NextCheckpoint >= _checkpoints.Count)
                {
                    FinishRacer(racer);

                    if (racer.IsPlayer)
                    {
                        return;
                    }

                    continue;
                }

                racer.Ai?.SetTarget(_checkpoints[racer.NextCheckpoint]);

                if (racer.IsPlayer)
                {
                    _waypointIndicator?.SetTarget(_checkpoints[racer.NextCheckpoint]);
                    _eventBus.Publish(new RaceProgressEvent(racer.NextCheckpoint + 1, _checkpoints.Count));
                }
            }
        }

        private void TickCountdown()
        {
            _countdownRemaining -= Time.unscaledDeltaTime;

            var currentSecond = Mathf.CeilToInt(_countdownRemaining);
            if (currentSecond < _lastAnnouncedSecond && currentSecond > 0)
            {
                _lastAnnouncedSecond = currentSecond;
                _eventBus.Publish(new RaceCountdownEvent(currentSecond));
            }

            if (_countdownRemaining <= 0f)
            {
                BeginRacing();
            }
        }

        private void FinishRacer(Racer racer)
        {
            racer.Finished = true;
            racer.Ai?.ClearTarget();
            _finishedCount++;

            if (!racer.IsPlayer)
            {
                return;
            }

            var place = _finishedCount;
            ApplyPlaceReward(place);
            _eventBus.Publish(new RaceFinishedEvent(place, _racers.Count));
            EndRace();
        }

        private void ApplyPlaceReward(int place)
        {
            if (_race.PlaceRewards == null || place - 1 >= _race.PlaceRewards.Length)
            {
                return;
            }

            var reward = _race.PlaceRewards[place - 1];
            if (reward?.Effects == null)
            {
                return;
            }

            foreach (var effect in reward.Effects)
            {
                if (effect != null)
                {
                    effect.Apply(_gameContext);
                }
            }
        }

        private void EndRace()
        {
            foreach (var spawned in _spawned)
            {
                if (spawned != null)
                {
                    Object.Destroy(spawned);
                }
            }

            _spawned.Clear();
            _racers.Clear();
            _checkpoints.Clear();
            _markers.Clear();
            _waypointIndicator?.ClearTarget();
            _waypointIndicator = null;
            _active = false;
        }
    }
}
