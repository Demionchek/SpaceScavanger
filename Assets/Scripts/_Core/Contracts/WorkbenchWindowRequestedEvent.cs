namespace Game.Core
{
    public readonly struct WorkbenchWindowRequestedEvent
    {
        public readonly CraftingRecipe[] Recipes;

        public WorkbenchWindowRequestedEvent(CraftingRecipe[] recipes)
        {
            Recipes = recipes;
        }
    }
}
