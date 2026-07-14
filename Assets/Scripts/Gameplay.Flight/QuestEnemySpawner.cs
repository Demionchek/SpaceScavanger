using System;
using System.Collections.Generic;
using Game.Core;
using Game.Gameplay.Shared;
using UnityEngine;
using VContainer.Unity;
using Random = UnityEngine.Random;

namespace Game.Gameplay.Flight
{
    public sealed class QuestEnemySpawner : IStartable, IDisposable
    {
        private const int CenterAttempts = 24;
        private const int PointAttempts = 32;

        private readonly EventBus _eventBus;
        private readonly LifetimeScope _rootScope;
        private readonly PlayerMarker _player;

        public QuestEnemySpawner(EventBus eventBus, LifetimeScope rootScope, PlayerMarker player)
        {
            _eventBus = eventBus;
            _rootScope = rootScope;
            _player = player;
        }

        public void Start()
        {
            _eventBus.Subscribe<EnemyGroupSpawnRequestedEvent>(OnSpawnRequested);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<EnemyGroupSpawnRequestedEvent>(OnSpawnRequested);
        }

        private void OnSpawnRequested(EnemyGroupSpawnRequestedEvent evt)
        {
            if (evt.Prefab == null || evt.Count <= 0 || _player == null)
            {
                return;
            }

            Vector2 playerPosition = _player.transform.position;
            var groupRadius = Mathf.Max(evt.MinSpacing * evt.Count * 0.5f, evt.MinSpacing);
            var center = FindGroupCenter(playerPosition, groupRadius, evt);

            var placed = new List<Vector2>(evt.Count);
            for (var i = 0; i < evt.Count; i++)
            {
                var position = FindSpawnPoint(center, groupRadius, placed, evt);
                placed.Add(position);
                SpawnEnemy(evt, position);
            }
        }

        // Ищем центр группы: свободная точка на заданной дистанции от игрока,
        // с местом под всю группу. После всех неудачных попыток берём последнюю —
        // квест важнее идеальной расстановки.
        private static Vector2 FindGroupCenter(Vector2 playerPosition, float groupRadius, EnemyGroupSpawnRequestedEvent evt)
        {
            Vector2 candidate = playerPosition + Vector2.right * evt.MaxDistanceFromPlayer;

            for (var attempt = 0; attempt < CenterAttempts; attempt++)
            {
                var direction = Random.insideUnitCircle.normalized;
                if (direction == Vector2.zero)
                {
                    direction = Vector2.right;
                }

                var distance = Random.Range(evt.MinDistanceFromPlayer, evt.MaxDistanceFromPlayer);
                candidate = playerPosition + direction * distance;

                if (Physics2D.OverlapCircle(candidate, evt.ClearRadius + groupRadius * 0.5f) == null)
                {
                    return candidate;
                }
            }

            return candidate;
        }

        // Точка внутри группы: не ближе MinSpacing к уже расставленным и с
        // физически свободным пятном ClearRadius. Если идеальной точки нет —
        // берём лучшую из проверенных (максимально удалённую и свободную).
        private static Vector2 FindSpawnPoint(
            Vector2 center,
            float groupRadius,
            List<Vector2> placed,
            EnemyGroupSpawnRequestedEvent evt)
        {
            var best = center;
            var bestScore = float.MinValue;

            for (var attempt = 0; attempt < PointAttempts; attempt++)
            {
                var candidate = center + Random.insideUnitCircle * groupRadius;
                var minDistance = MinDistanceTo(placed, candidate);
                var areaFree = Physics2D.OverlapCircle(candidate, evt.ClearRadius) == null;

                if (areaFree && minDistance >= evt.MinSpacing)
                {
                    return candidate;
                }

                var score = minDistance + (areaFree ? 1000f : 0f);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }

            return best;
        }

        private static float MinDistanceTo(List<Vector2> placed, Vector2 candidate)
        {
            var min = float.MaxValue;
            foreach (var position in placed)
            {
                min = Mathf.Min(min, Vector2.Distance(position, candidate));
            }

            return min;
        }

        private void SpawnEnemy(EnemyGroupSpawnRequestedEvent evt, Vector2 position)
        {
            var prefabScope = evt.Prefab.GetComponent<LifetimeScope>();
            var instanceScope = _rootScope.CreateChildFromPrefab(prefabScope);
            instanceScope.transform.SetParent(null);
            instanceScope.transform.position = position;

            if (string.IsNullOrEmpty(evt.GroupTag))
            {
                return;
            }

            var groupTag = instanceScope.GetComponentInChildren<EnemyGroupTag>();
            if (groupTag != null)
            {
                groupTag.SetGroupTag(evt.GroupTag);
            }
            else
            {
                Debug.LogWarning($"Enemy prefab '{evt.Prefab.name}' has no EnemyGroupTag — quest kills won't count");
            }
        }
    }
}
