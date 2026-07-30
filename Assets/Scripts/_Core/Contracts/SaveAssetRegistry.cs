using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(menuName = "Game/Save/Save Asset Registry", fileName = "SaveAssetRegistry")]
    public sealed class SaveAssetRegistry : ScriptableObject
    {
        [SerializeField] private NpcGroup[] _npcGroups;
        [SerializeField] private ItemDefinition[] _items;
        [SerializeField] private CraftingRecipe[] _recipes;
        [SerializeField] private QuestDefinition[] _quests;

        public IReadOnlyList<NpcGroup> NpcGroups => _npcGroups;

        public string GetId(Object asset) => asset != null ? asset.name : null;

        public NpcGroup GetNpcGroup(string id) => Find(_npcGroups, id);
        public ItemDefinition GetItem(string id) => Find(_items, id);
        public CraftingRecipe GetRecipe(string id) => Find(_recipes, id);
        public QuestDefinition GetQuest(string id) => Find(_quests, id);

        private static T Find<T>(T[] array, string id) where T : Object
        {
            if (array == null || string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (var asset in array)
            {
                if (asset != null && asset.name == id)
                {
                    return asset;
                }
            }

            return null;
        }
    }
}
