using Game.Core;

namespace Game.Gameplay.Shared
{
    public sealed class CraftingService : ICraftingService
    {
        private readonly IResourceService _resources;
        private readonly IItemService _items;
        private readonly EventBus _eventBus;

        public CraftingService(IResourceService resources, IItemService items, EventBus eventBus)
        {
            _resources = resources;
            _items = items;
            _eventBus = eventBus;
        }

        public bool CanCraft(CraftingRecipe recipe)
        {
            if (recipe == null || recipe.Output == null)
            {
                return false;
            }

            foreach (var cost in recipe.Costs)
            {
                if (_resources.GetAmount(cost.Type) < cost.Amount)
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryCraft(CraftingRecipe recipe)
        {
            if (!CanCraft(recipe))
            {
                return false;
            }

            foreach (var cost in recipe.Costs)
            {
                _resources.TrySpend(cost.Type, cost.Amount);
            }

            _items.Add(recipe.Output, recipe.OutputCount);
            _eventBus.Publish(new ItemCraftedEvent(recipe.Output));
            return true;
        }
    }
}
