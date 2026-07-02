using UnityEngine;

namespace Game.Gameplay.Ship
{
    [RequireComponent(typeof(CharacterMovementController))]
    public sealed class CharacterAnimationController : MonoBehaviour
    {
        private static readonly int SpeedParam = Animator.StringToHash("Speed");
        private static readonly int IsGroundedParam = Animator.StringToHash("IsGrounded");
        private static readonly int VerticalVelocityParam = Animator.StringToHash("VerticalVelocity");

        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private float _flipThreshold = 0.01f;

        private CharacterMovementController _movement;

        private void Awake()
        {
            _movement = GetComponent<CharacterMovementController>();
        }

        private void Update()
        {
            _animator.SetFloat(SpeedParam, Mathf.Abs(_movement.HorizontalVelocity));
            _animator.SetFloat(VerticalVelocityParam, _movement.VerticalVelocity);
            _animator.SetBool(IsGroundedParam, _movement.IsGrounded);

            if (_movement.HorizontalVelocity > _flipThreshold)
            {
                _spriteRenderer.flipX = false;
            }
            else if (_movement.HorizontalVelocity < -_flipThreshold)
            {
                _spriteRenderer.flipX = true;
            }
        }
    }
}
