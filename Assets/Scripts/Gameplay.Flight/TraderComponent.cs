using Game.Core;
using Game.Data;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Flight
{
    public sealed class TraderComponent : MonoBehaviour, IInteractable
    {
        [SerializeField] private TraderInventory _inventory;
        [SerializeField] private string _prompt = "Trade";

        private EventBus _eventBus;

        [Inject]
        public void Construct(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public string Prompt => _prompt;

        public void Interact(PlayerContext ctx)
        {
            _eventBus.Publish(new TradeWindowRequestedEvent(_inventory));
        }
    }
}
