using UnityEngine;

namespace Game.Gameplay.Flight
{
    public sealed class CheckpointMarker : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _indicator;
        [SerializeField] private Color _pendingColor = Color.red;
        [SerializeField] private Color _passedColor = Color.green;

        private void Awake()
        {
            _indicator.color = _pendingColor;
        }

        public void SetPassed()
        {
            _indicator.color = _passedColor;
        }
    }
}
