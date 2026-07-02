using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Ship
{
    public sealed class WorkbenchStub : MonoBehaviour, IInteractable
    {
        [SerializeField] private ResourceType _resourceType = ResourceType.Scrap;
        [SerializeField] private int _amountPerInteract = 1;

        public string Prompt => "Workbench (stub)";

        public void Interact(PlayerContext ctx)
        {
            ctx.ResourceService.Add(_resourceType, _amountPerInteract);
        }
    }
}
