using Game.Core;

namespace Game.Gameplay.Shared
{
    public sealed class TradeService : ITradeService
    {
        private readonly IResourceService _resources;
        private readonly IItemService _items;
        private readonly IRecipeService _recipes;

        public TradeService(IResourceService resources, IItemService items, IRecipeService recipes)
        {
            _resources = resources;
            _items = items;
            _recipes = recipes;
        }

        public bool BuyRecipe(CraftingRecipe recipe, int price)
        {
            if (recipe == null || _recipes.Knows(recipe))
            {
                return false;
            }

            if (!_resources.TrySpend(ResourceType.Credits, price))
            {
                return false;
            }

            _recipes.Learn(recipe);
            return true;
        }

        public bool BuyResource(ResourceType resource, int unitPrice, int quantity)
        {
            if (!_resources.TrySpend(ResourceType.Credits, unitPrice * quantity))
            {
                return false;
            }

            _resources.Add(resource, quantity);
            return true;
        }

        public bool SellResource(ResourceType resource, int unitPrice, int quantity)
        {
            if (!_resources.TrySpend(resource, quantity))
            {
                return false;
            }

            _resources.Add(ResourceType.Credits, unitPrice * quantity);
            return true;
        }

        public bool BuyItem(ItemDefinition item, int unitPrice, int quantity)
        {
            if (!_resources.TrySpend(ResourceType.Credits, unitPrice * quantity))
            {
                return false;
            }

            _items.Add(item, quantity);
            return true;
        }

        public bool SellItem(ItemDefinition item, int unitPrice, int quantity)
        {
            if (!_items.TrySpend(item, quantity))
            {
                return false;
            }

            _resources.Add(ResourceType.Credits, unitPrice * quantity);
            return true;
        }
    }
}
