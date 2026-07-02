using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Flight
{
    [RequireComponent(typeof(ShipMovementController))]
    public sealed class EngineVisualsController : MonoBehaviour
    {
        [SerializeField] private List<GameObject> _engineParticles;
        [SerializeField] private float _activationThreshold = 0.1f;

        private ShipMovementController _movement;

        private void Awake()
        {
            _movement = GetComponent<ShipMovementController>();
        }

        private void Update()
        {
            var isThrusting = Mathf.Abs(_movement.CurrentThrottle) > _activationThreshold;

            if (isThrusting && !_engineParticles[0].activeInHierarchy)
            {
                _engineParticles.ForEach(particle => particle.gameObject.SetActive(true));
            }
            else if (!isThrusting && _engineParticles[0].activeInHierarchy)
            {
                _engineParticles.ForEach(particle => particle.gameObject.SetActive(false));
            }
        }
    }
}
