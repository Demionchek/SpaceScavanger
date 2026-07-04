using System;
using Game.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Game.Gameplay.Flight
{
    public sealed class HookableResource : MonoBehaviour, IHookable
    {
        [SerializeField] private int _requiredHookLevel;
        [SerializeField] private ResourceType _resourceType;
        [SerializeField] private bool _randomAmount;
        [SerializeField] private int _minAmount = 1;
        [SerializeField] private int _maxAmount = 1;
        [SerializeField] private float _minRotatationAngle = 1;
        [SerializeField] private float _maxRotatationAngle = 10;
        [SerializeField] private Color _color = Color.white;
        [SerializeField] private GameObject[] _visualVariants;
        [SerializeField] private Rigidbody2D _rigidbody;

        public int RequiredHookLevel => _requiredHookLevel;
        public Vector2 Position => transform.position;

        private float _rotatationAngle;

        private void Awake()
        {
            SpawnVisual();
            _rotatationAngle = Random.Range(_minRotatationAngle, _maxRotatationAngle);
        }

        private void FixedUpdate()
        {
            _rigidbody.MoveRotation(_rigidbody.rotation + _rotatationAngle * Time.fixedDeltaTime);
        }

        private void SpawnVisual()
        {
            if (_visualVariants == null || _visualVariants.Length == 0)
            {
                return;
            }

            var variant = _visualVariants[Random.Range(0, _visualVariants.Length)];
            var visual = Instantiate(variant, transform);
            visual.transform.localPosition = Vector3.zero;

            foreach (var spriteRenderer in visual.GetComponentsInChildren<SpriteRenderer>())
            {
                spriteRenderer.color = _color;
            }
        }

        public void OnGrabbed(HookContext ctx)
        {
            var amount = _randomAmount ? Random.Range(_minAmount, _maxAmount + 1) : _minAmount;
            ctx.ResourceService.Add(_resourceType, amount);
            Destroy(gameObject);
        }
    }
}
