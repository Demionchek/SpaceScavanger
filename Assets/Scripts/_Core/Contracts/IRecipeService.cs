using System.Collections.Generic;

namespace Game.Core
{
    public readonly struct RecipeLearnedEvent
    {
        public readonly CraftingRecipe Recipe;

        public RecipeLearnedEvent(CraftingRecipe recipe)
        {
            Recipe = recipe;
        }
    }

    public interface IRecipeService
    {
        IReadOnlyList<CraftingRecipe> Known { get; }
        bool Knows(CraftingRecipe recipe);
        bool Learn(CraftingRecipe recipe);
    }
}
