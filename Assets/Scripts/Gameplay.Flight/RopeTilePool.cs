using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Flight
{
    public sealed class RopeTilePool
    {
        private readonly GameObject _tilePrefab;
        private readonly Transform _parent;
        private readonly List<Transform> _tiles = new();

        public RopeTilePool(GameObject tilePrefab, Transform parent)
        {
            _tilePrefab = tilePrefab;
            _parent = parent;
        }

        public void Draw(Vector2 start, Vector2 end, float tileLength)
        {
            var distance = Vector2.Distance(start, end);
            var tileCount = tileLength > 0f ? Mathf.FloorToInt(distance / tileLength) : 0;
            var direction = (end - start).normalized;

            EnsureCapacity(tileCount);

            for (var i = 0; i < _tiles.Count; i++)
            {
                var active = i < tileCount;
                _tiles[i].gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                _tiles[i].position = start + direction * (tileLength * (i + 0.5f));
                _tiles[i].rotation = Quaternion.FromToRotation(Vector3.right, direction);
            }
        }

        public void Clear()
        {
            foreach (var tile in _tiles)
            {
                tile.gameObject.SetActive(false);
            }
        }

        private void EnsureCapacity(int count)
        {
            while (_tiles.Count < count)
            {
                _tiles.Add(Object.Instantiate(_tilePrefab, _parent).transform);
            }
        }
    }
}
