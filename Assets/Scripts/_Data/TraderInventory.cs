using System;
using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public sealed class ResourceTradeOffer
    {
        public ResourceType Resource;
        public int BuyPrice;
        public int SellPrice;
    }

    [Serializable]
    public sealed class ItemTradeOffer
    {
        public ItemDefinition Item;
        public int BuyPrice;
        public int SellPrice;
    }

    [Serializable]
    public sealed class RecipeTradeOffer
    {
        public CraftingRecipe Recipe;
        public int Price;
    }

    [CreateAssetMenu(menuName = "Game/Trader/Trader Inventory", fileName = "TraderInventory")]
    public sealed class TraderInventory : ScriptableObject
    {
        public ResourceTradeOffer[] ResourceOffers;
        public ItemTradeOffer[] ItemOffers;
        public RecipeTradeOffer[] RecipeOffers;
    }
}
