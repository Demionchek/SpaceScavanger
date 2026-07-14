using System;
using UnityEngine;

namespace Game.Core
{
    [Serializable]
    public struct ResourceCost
    {
        public ResourceType Type;
        public int Amount;
    }

    [CreateAssetMenu(menuName = "Game/Craft/Crafting Recipe", fileName = "CraftingRecipe")]
    public sealed class CraftingRecipe : ScriptableObject
    {
        public ResourceCost[] Costs;
        public ItemDefinition Output;
        public int OutputCount = 1;
    }
}
