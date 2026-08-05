using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Ship
{
    public sealed class ShipComputerComponent : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _prompt = "Ship computer";
        [SerializeField] private string _unreadPrompt = "Unread message";
        [SerializeField] private GameObject _unreadIndicator;

        private EventBus _eventBus;
        private IStoryDialogueService _storyDialogues;

        [Inject]
        public void Construct(EventBus eventBus, IStoryDialogueService storyDialogues)
        {
            _eventBus = eventBus;
            _storyDialogues = storyDialogues;

            _eventBus.Subscribe<StoryDialoguePendingEvent>(OnMessageIncoming);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<StoryDialoguePendingEvent>(OnMessageIncoming);
        }

        private void Start()
        {
            SetUnread(HasUnread);
        }

        private bool HasUnread => _storyDialogues.HasPendingFor(StoryDialogueDelivery.ShipComputer);

        public string Prompt => HasUnread ? _unreadPrompt : _prompt;

        public void Interact(PlayerContext ctx)
        {
            if (_storyDialogues.TryTakePending(StoryDialogueDelivery.ShipComputer, out var node))
            {
                SetUnread(false);
                _eventBus.Publish(new DialogueRequestedEvent(node));
                return;
            }

            _eventBus.Publish(new ShipInfoWindowRequestedEvent());
        }

        private void OnMessageIncoming(StoryDialoguePendingEvent evt)
        {
            if (evt.Delivery == StoryDialogueDelivery.ShipComputer)
            {
                SetUnread(true);
            }
        }

        private void SetUnread(bool unread)
        {
            if (_unreadIndicator != null)
            {
                _unreadIndicator.SetActive(unread);
            }
        }
    }
}
