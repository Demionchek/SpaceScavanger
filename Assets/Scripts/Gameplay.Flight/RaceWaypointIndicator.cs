using UnityEngine;

namespace Game.Gameplay.Flight
{
    // Стрелка на орбите корабля игрока, всегда смотрит на следующий чекпоинт.
    // Спрайт стрелки должен смотреть вдоль +X.
    public sealed class RaceWaypointIndicator : MonoBehaviour
    {
        [SerializeField] private Transform _indicator;
        [SerializeField] private Transform _anchor;
        [SerializeField] private float _radius = 2f;

        private Vector2 _target;
        private bool _hasTarget;

        private void Awake()
        {
            _indicator.gameObject.SetActive(false);
        }

        public void SetTarget(Vector2 target)
        {
            _target = target;
            _hasTarget = true;
            _indicator.gameObject.SetActive(true);
        }

        public void ClearTarget()
        {
            _hasTarget = false;
            _indicator.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!_hasTarget)
            {
                return;
            }

            Vector2 origin = _anchor != null ? _anchor.position : transform.position;
            var direction = (_target - origin).normalized;

            _indicator.position = origin + direction * _radius;
            _indicator.rotation = Quaternion.FromToRotation(Vector3.right, direction);
        }
    }
}
