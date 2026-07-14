using System.Collections.Generic;

namespace Game.Core
{
    public interface IItemService
    {
        IEnumerable<KeyValuePair<ItemDefinition, int>> All { get; }
        int GetAmount(ItemDefinition item);
        void Add(ItemDefinition item, int amount);
        bool TrySpend(ItemDefinition item, int amount);
    }
}
