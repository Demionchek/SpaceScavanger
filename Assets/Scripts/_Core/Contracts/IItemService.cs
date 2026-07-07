namespace Game.Core
{
    public interface IItemService
    {
        int GetAmount(ItemDefinition item);
        void Add(ItemDefinition item, int amount);
        bool TrySpend(ItemDefinition item, int amount);
    }
}
