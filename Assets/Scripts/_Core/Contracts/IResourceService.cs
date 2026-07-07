namespace Game.Core
{
    public interface IResourceService
    {
        int GetAmount(ResourceType type);
        void Add(ResourceType type, int amount);
        bool TrySpend(ResourceType type, int amount);
    }
}
