namespace Game.Core
{
    public interface IResourceService
    {
        int GetAmount(ResourceType type);
        void Add(ResourceType type, int amount);
    }
}
