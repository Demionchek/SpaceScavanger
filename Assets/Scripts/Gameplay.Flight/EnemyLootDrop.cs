using Game.Gameplay.Shared;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Gameplay.Flight
{
    [RequireComponent(typeof(Health))]
    public sealed class EnemyLootDrop : MonoBehaviour
    {
        [SerializeField] private GameObject[] _lootPrefabs;
        [SerializeField] private int _minDrops = 1;
        [SerializeField] private int _maxDrops = 2;

        private void Awake()
        {
            GetComponent<Health>().Died += OnDied;
        }

        private void OnDied()
        {
            if (_lootPrefabs.Length == 0)
            {
                return;
            }

            var count = Random.Range(_minDrops, _maxDrops + 1);
            for (var i = 0; i < count; i++)
            {
                var prefab = _lootPrefabs[Random.Range(0, _lootPrefabs.Length)];
                Instantiate(prefab, transform.position, Quaternion.identity);
            }
        }
    }
}
