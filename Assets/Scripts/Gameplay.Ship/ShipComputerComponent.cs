using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Ship
{
    public sealed class ShipComputerComponent : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _prompt = "Ship computer";

        private EventBus _eventBus;

        [Inject]
        public void Construct(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public string Prompt => _prompt;

        public void Interact(PlayerContext ctx)
        {
            _eventBus.Publish(new ShipInfoWindowRequestedEvent());
        }
    }
}
