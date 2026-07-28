using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Flight
{
    public sealed class WormholeTravelController : MonoBehaviour
    {
        [SerializeField] private string _arrivalDialogueNode;

        private EventBus _eventBus;
        private PlayerMarker _player;
        private ZoneBounds _bounds;
        private bool _pending;

        [Inject]
        public void Construct(EventBus eventBus, PlayerMarker player, ZoneBounds bounds)
        {
            _eventBus = eventBus;
            _player = player;
            _bounds = bounds;

            _eventBus.Subscribe<WormholeTravelRequestedEvent>(OnTravelRequested);
            _eventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);
        }

        private void OnDestroy()
        {
            if (_eventBus == null)
            {
                return;
            }

            _eventBus.Unsubscribe<WormholeTravelRequestedEvent>(OnTravelRequested);
            _eventBus.Unsubscribe<GameStateChangedEvent>(OnStateChanged);
        }

        private void OnTravelRequested(WormholeTravelRequestedEvent _)
        {
            _pending = true;
        }

        private void OnStateChanged(GameStateChangedEvent evt)
        {
            if (!_pending || evt.NewState is not ShipInteriorState)
            {
                return;
            }

            _pending = false;

            var body = _player.GetComponentInChildren<Rigidbody2D>();
            if (body != null)
            {
                body.position = _bounds.Center;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
            else
            {
                _player.transform.position = _bounds.Center;
            }

            _eventBus.Publish(new ZoneRegenerateRequestedEvent());

            if (!string.IsNullOrEmpty(_arrivalDialogueNode))
            {
                _eventBus.Publish(new DialogueRequestedEvent(_arrivalDialogueNode));
            }
        }
    }
}
