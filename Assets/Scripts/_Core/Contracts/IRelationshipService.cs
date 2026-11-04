using System.Collections.Generic;

namespace Game.Core
{
    // Личное отношение конкретного персонажа к игроку — отдельно от репутации фракции.
    public interface IRelationshipService
    {
        IEnumerable<KeyValuePair<CharacterDefinition, int>> All { get; }
        int Get(CharacterDefinition character);
        void Add(CharacterDefinition character, int amount);
    }
}
