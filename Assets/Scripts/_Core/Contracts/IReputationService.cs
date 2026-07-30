using System.Collections.Generic;

namespace Game.Core
{
    public interface IReputationService
    {
        IEnumerable<KeyValuePair<NpcGroup, int>> All { get; }
        int GetReputation(NpcGroup group);
        void Add(NpcGroup group, int amount);
    }
}
