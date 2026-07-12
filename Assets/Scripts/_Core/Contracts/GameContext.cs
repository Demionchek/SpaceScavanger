namespace Game.Core
{
    public sealed class GameContext
    {
        public IResourceService ResourceService { get; }

        public GameContext(IResourceService resourceService)
        {
            ResourceService = resourceService;
        }
    }
}
