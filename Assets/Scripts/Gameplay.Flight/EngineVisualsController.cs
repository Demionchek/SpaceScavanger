using System.Collections.Generic;
using UnityEngine;

namespace Game.Gameplay.Flight
{
    [RequireComponent(typeof(ShipMovementController))]
    public sealed class EngineVisualsController : MonoBehaviour
    {
        [SerializeField] private List<GameObject> _forwardEngineParticles;
        [SerializeField] private List<GameObject> _backwardEngineParticles;
        [SerializeField] private float _activationThreshold = 0.1f;

        private ShipMovementController _movement;

        private void Awake()
        {
            _movement = GetComponent<ShipMovementController>();
        }

        private void Update()
        {
            var throttle = _movement.CurrentThrottle;
            var isThrusting = Mathf.Abs(throttle) > _activationThreshold;
            var isForward = throttle > 0f;

            SetParticlesActive(_forwardEngineParticles, isThrusting && isForward);
            SetParticlesActive(_backwardEngineParticles, isThrusting && !isForward);
        }

        private static void SetParticlesActive(List<GameObject> particles, bool active)
        {
            if (particles.Count == 0 || particles[0].activeInHierarchy == active)
            {
                return;
            }

            particles.ForEach(particle => particle.SetActive(active));
        }
    }
}
