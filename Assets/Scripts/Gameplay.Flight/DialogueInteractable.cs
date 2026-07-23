using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Flight
{
    // Универсальный NPC "поговорить": по F открывает указанную Yarn-ноду.
    public sealed class DialogueInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _dialogueNode;
        [SerializeField] private string _prompt = "Talk";

        private EventBus _eventBus;

        [Inject]
        public void Construct(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public string Prompt => _prompt;

        public void Interact(PlayerContext ctx)
        {
            _eventBus.Publish(new DialogueRequestedEvent(_dialogueNode));
        }
    }
}
