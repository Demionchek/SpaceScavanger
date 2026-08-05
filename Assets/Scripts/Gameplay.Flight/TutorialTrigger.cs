using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Flight
{
    public sealed class TutorialTrigger : MonoBehaviour
    {
        [SerializeField, TextArea] private string _message;
        [SerializeField] private LayerMask _playerMask;
        [SerializeField] private bool _showOnce = true;

        private EventBus _eventBus;
        private bool _shown;

        [Inject]
        public void Construct(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other) || (_showOnce && _shown))
            {
                return;
            }

            _shown = true;
            _eventBus.Publish(new TutorialPopupEvent(_message, true));
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsPlayer(other))
            {
                _eventBus.Publish(new TutorialPopupEvent(_message, false));
            }
        }

        private bool IsPlayer(Collider2D other) => (_playerMask.value & (1 << other.gameObject.layer)) != 0;
    }
}
