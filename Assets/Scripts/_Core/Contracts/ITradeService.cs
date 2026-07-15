namespace Game.Core
{
    public interface ITradeService
    {
        bool BuyResource(ResourceType resource, int unitPrice, int quantity);
        bool SellResource(ResourceType resource, int unitPrice, int quantity);
        bool BuyItem(ItemDefinition item, int unitPrice, int quantity);
        bool SellItem(ItemDefinition item, int unitPrice, int quantity);
        bool BuyRecipe(CraftingRecipe recipe, int price);
    }
}
