using UnityEngine;

namespace Game.Core
{
    public sealed class ZoneBounds : MonoBehaviour
    {
        [SerializeField] private float _safeRadius = 60f;
        [SerializeField] private float _warnMargin = 15f;
        [SerializeField] private float _deathDistance = 20f;

        public Vector2 Center => transform.position;
        public float SafeRadius => _safeRadius;
        public float WarnMargin => _warnMargin;
        public float DeathDistance => _deathDistance;
    }
}
