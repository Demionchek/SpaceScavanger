using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Effects/Spawn Enemy Group", fileName = "SpawnEnemyGroupEffect")]
    public sealed class SpawnEnemyGroupEffect : ChoiceEffect
    {
        [SerializeField] private GameObject _enemyPrefab;
        [SerializeField] private int _count = 3;
        [SerializeField] private string _groupTag;
        [SerializeField] private float _minDistanceFromPlayer = 15f;
        [SerializeField] private float _maxDistanceFromPlayer = 25f;
        [SerializeField] private float _minSpacing = 3f;
        [SerializeField] private float _clearRadius = 1.5f;

        public override void Apply(GameContext ctx)
        {
            ctx.EventBus.Publish(new EnemyGroupSpawnRequestedEvent(
                _enemyPrefab,
                _count,
                _groupTag,
                _minDistanceFromPlayer,
                _maxDistanceFromPlayer,
                _minSpacing,
                _clearRadius));
        }
    }
}
