using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Flight
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class ShipMovementController : MonoBehaviour
    {
        [SerializeField] private float _thrustForce = 10f;
        [SerializeField] private float _torqueStrength = 90f;

        private Rigidbody2D _rigidbody;
        private IShipInputProvider _shipInput;

        public float CurrentThrottle => _shipInput.Throttle;

        [Inject]
        public void Construct(IShipInputProvider shipInput)
        {
            _shipInput = shipInput;
        }

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.gravityScale = 0f;
        }

        // Assumes the ship sprite faces +X (transform.right = nose/forward).
        private void FixedUpdate()
        {
            _rigidbody.AddForce(transform.right * (_shipInput.Throttle * _thrustForce));
            _rigidbody.AddTorque(_shipInput.Rotation * _torqueStrength);
        }
    }
}
