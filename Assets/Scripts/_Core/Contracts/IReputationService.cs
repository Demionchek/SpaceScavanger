namespace Game.Core
{
    public interface IReputationService
    {
        int GetReputation(NpcGroup group);
        void Add(NpcGroup group, int amount);
    }
}
