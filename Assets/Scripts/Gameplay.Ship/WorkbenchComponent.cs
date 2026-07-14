using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Ship
{
    public sealed class WorkbenchComponent : MonoBehaviour, IInteractable
    {
        [SerializeField] private CraftingRecipe[] _recipes;
        [SerializeField] private string _prompt = "Workbench";

        private EventBus _eventBus;

        [Inject]
        public void Construct(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public string Prompt => _prompt;

        public void Interact(PlayerContext ctx)
        {
            _eventBus.Publish(new WorkbenchWindowRequestedEvent(_recipes));
        }
    }
}
