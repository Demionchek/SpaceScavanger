using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Flight
{
    public sealed class HookProjectile : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D _rigidbody;

        private HookController _owner;
        private LayerMask _hookableMask;
        private int _hookLevel;
        private float _maxRange;
        private float _speed;
        private bool _returning;

        public void Launch(HookController owner, Vector2 direction, float range, float speed, LayerMask hookableMask, int hookLevel)
        {
            _owner = owner;
            _hookableMask = hookableMask;
            _hookLevel = hookLevel;
            _maxRange = range;
            _speed = speed;
            _rigidbody.linearVelocity = direction.normalized * speed;
        }

        private void FixedUpdate()
        {
            if (_returning)
            {
                var toOwner = _owner.HookAnchorPosition - (Vector2)transform.position;
                if (toOwner.magnitude <= 0.2f)
                {
                    _owner.OnHookReturned(this);
                    return;
                }

                _rigidbody.linearVelocity = toOwner.normalized * _speed;
                return;
            }

            if (Vector2.Distance(_owner.HookAnchorPosition, transform.position) >= _maxRange)
            {
                StartReturning();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_returning)
            {
                return;
            }

            if (((1 << other.gameObject.layer) & _hookableMask) == 0)
            {
                return;
            }

            if (!other.TryGetComponent<IHookable>(out var hookable))
            {
                return;
            }

            if (_hookLevel >= hookable.RequiredHookLevel)
            {
                _owner.OnHookGrabbed(hookable);
            }
            else
            {
                Debug.Log($"Hook level {_hookLevel} too low for resource requiring {hookable.RequiredHookLevel}");
            }

            StartReturning();
        }

        private void StartReturning()
        {
            _returning = true;
            _rigidbody.linearVelocity = Vector2.zero;
        }
    }
}
