namespace Game.Core
{
    public interface ICraftingService
    {
        bool CanCraft(CraftingRecipe recipe);
        bool TryCraft(CraftingRecipe recipe);
    }
}
