using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Game.Gameplay.Ship
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class CharacterMovementController : MonoBehaviour
    {
        [SerializeField] private float _moveSpeed = 4f;
        [SerializeField] private float _jumpForce = 8f;
        [SerializeField] private Transform _groundCheck;
        [SerializeField] private float _groundCheckRadius = 0.1f;
        [SerializeField] private LayerMask _groundMask;

        private Rigidbody2D _rigidbody;
        private InputAction _moveAction;
        private InputAction _jumpAction;

        public float HorizontalVelocity => _rigidbody.linearVelocity.x;
        public float VerticalVelocity => _rigidbody.linearVelocity.y;
        public bool IsGrounded { get; private set; }

        [Inject]
        public void Construct(InputActionAsset inputActions)
        {
            var platformerMap = inputActions.FindActionMap("Platformer", throwIfNotFound: true);
            _moveAction = platformerMap.FindAction("Move", throwIfNotFound: true);
            _jumpAction = platformerMap.FindAction("Jump", throwIfNotFound: true);
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            IsGrounded = Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, _groundMask);

            if (IsGrounded && _jumpAction.WasPressedThisFrame())
            {
                _rigidbody.AddForce(Vector2.up * _jumpForce, ForceMode2D.Impulse);
            }
        }

        private void FixedUpdate()
        {
            var horizontalInput = _moveAction.ReadValue<Vector2>().x;
            _rigidbody.linearVelocity = new Vector2(horizontalInput * _moveSpeed, _rigidbody.linearVelocity.y);
        }
    }
}
