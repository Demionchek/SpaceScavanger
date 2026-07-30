using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    public sealed class RecipeService : IRecipeService, ISaveable
    {
        private readonly EventBus _eventBus;
        private readonly SaveAssetRegistry _registry;
        private readonly List<CraftingRecipe> _known = new();

        public RecipeService(EventBus eventBus, SaveAssetRegistry registry)
        {
            _eventBus = eventBus;
            _registry = registry;
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

        public string SaveId => "recipes";

        public string Save()
        {
            var data = new SaveData();
            foreach (var recipe in _known)
            {
                data.recipes.Add(_registry.GetId(recipe));
            }

            return JsonUtility.ToJson(data);
        }

        public void Load(string json)
        {
            _known.Clear();
            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                return;
            }

            foreach (var id in data.recipes)
            {
                var recipe = _registry.GetRecipe(id);
                if (recipe != null && !_known.Contains(recipe))
                {
                    _known.Add(recipe);
                    _eventBus.Publish(new RecipeLearnedEvent(recipe));
                }
            }
        }

        [Serializable]
        private sealed class SaveData
        {
            public List<string> recipes = new();
        }
    }
}
