using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Flight
{
    // Пунктирный прицел из спрайтов от дула вдоль курса. Точки бледнеют
    // с расстоянием, линия обрывается на препятствии. Цвет всей линии
    // зависит от цели под прицелом: враг — красный, нейтрал — жёлтый,
    // друг — зелёный, иначе серый.
    public sealed class AimLineController : MonoBehaviour
    {
        [SerializeField] private Transform _muzzle;
        [SerializeField] private SpriteRenderer _dotPrefab;
        [SerializeField] private int _dotCount = 12;
        [SerializeField] private float _dotSpacing = 0.8f;
        [SerializeField] private float _nearAlpha = 0.8f;
        [SerializeField] private float _farAlpha = 0.1f;
        [SerializeField] private LayerMask _blockingMask = ~0;
        [SerializeField] private LayerMask _neutralMask;
        [SerializeField] private LayerMask _friendMask;
        [SerializeField] private Color _defaultColor = Color.gray;
        [SerializeField] private Color _enemyColor = Color.red;
        [SerializeField] private Color _neutralColor = Color.yellow;
        [SerializeField] private Color _friendColor = Color.green;

        private SpriteRenderer[] _dots;

        private void Awake()
        {
            _dots = new SpriteRenderer[_dotCount];
            for (var i = 0; i < _dotCount; i++)
            {
                var dot = Instantiate(_dotPrefab, _muzzle);
                dot.transform.localPosition = new Vector3(_dotSpacing * (i + 1), 0f, 0f);
                dot.transform.localRotation = Quaternion.identity;
                _dots[i] = dot;
            }
        }

        private void LateUpdate()
        {
            var maxDistance = _dotSpacing * _dotCount;
            var hit = FindHit(maxDistance);
            var color = ClassifyHit(hit);
            var visibleDistance = hit.collider != null ? hit.distance : maxDistance;

            for (var i = 0; i < _dots.Length; i++)
            {
                var dotDistance = _dotSpacing * (i + 1);
                var dot = _dots[i];

                if (dotDistance > visibleDistance)
                {
                    dot.enabled = false;
                    continue;
                }

                dot.enabled = true;
                color.a = Mathf.Lerp(_nearAlpha, _farAlpha, dotDistance / maxDistance);
                dot.color = color;
            }
        }

        private RaycastHit2D FindHit(float maxDistance)
        {
            var hits = Physics2D.RaycastAll(_muzzle.position, _muzzle.right, maxDistance, _blockingMask);
            foreach (var hit in hits)
            {
                if (!hit.collider.transform.IsChildOf(transform.root))
                {
                    return hit;
                }
            }

            return default;
        }

        private Color ClassifyHit(RaycastHit2D hit)
        {
            if (hit.collider == null)
            {
                return _defaultColor;
            }

            var layerBit = 1 << hit.collider.gameObject.layer;

            if ((_friendMask.value & layerBit) != 0)
            {
                return _friendColor;
            }

            if ((_neutralMask.value & layerBit) != 0)
            {
                return _neutralColor;
            }

            if (hit.collider.TryGetComponent<IDamageable>(out _))
            {
                return _enemyColor;
            }

            return _defaultColor;
        }
    }
}
