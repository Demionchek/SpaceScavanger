using Game.Core;

namespace Game.Gameplay.Shared
{
    public sealed class CraftingService : ICraftingService
    {
        private readonly IResourceService _resources;
        private readonly IItemService _items;
        private readonly IUpgradeService _upgrades;
        private readonly EventBus _eventBus;

        public CraftingService(IResourceService resources, IItemService items, IUpgradeService upgrades,
            EventBus eventBus)
        {
            _resources = resources;
            _items = items;
            _upgrades = upgrades;
            _eventBus = eventBus;
        }

        public bool CanCraft(CraftingRecipe recipe)
        {
            if (recipe == null || recipe.Output == null)
            {
                return false;
            }

            // Улучшение одно на корабль, поэтому второй экземпляр не крафтится —
            // ни пока первый лежит в трюме, ни после установки.
            if (recipe.Output is UpgradeItemDefinition upgrade &&
                (_upgrades.IsInstalled(upgrade) || _items.GetAmount(upgrade) > 0))
            {
                return false;
            }

            // Ступень крюка собирается только поверх предыдущей: иначе игрок собрал бы
            // «Hook 4», получил свой +1 и не понял, почему уран всё равно не берётся.
            if (recipe.RequiredUpgrade != null && !_upgrades.IsInstalled(recipe.RequiredUpgrade))
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
