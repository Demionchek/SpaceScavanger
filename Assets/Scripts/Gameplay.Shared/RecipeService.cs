using System.Collections.Generic;
using Game.Core;

namespace Game.Gameplay.Shared
{
    public sealed class RecipeService : IRecipeService
    {
        private readonly EventBus _eventBus;
        private readonly List<CraftingRecipe> _known = new();

        public RecipeService(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public IReadOnlyList<CraftingRecipe> Known => _known;

        public bool Knows(CraftingRecipe recipe)
        {
            return _known.Contains(recipe);
        }

        public bool Learn(CraftingRecipe recipe)
        {
            if (recipe == null || Knows(recipe))
            {
                return false;
            }

            _known.Add(recipe);
            _eventBus.Publish(new RecipeLearnedEvent(recipe));
            return true;
        }
    }
}
