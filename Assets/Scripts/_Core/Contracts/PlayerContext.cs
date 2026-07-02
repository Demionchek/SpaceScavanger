namespace Game.Core
{
    public sealed class PlayerContext
    {
        public IResourceService ResourceService { get; }

        public PlayerContext(IResourceService resourceService)
        {
            ResourceService = resourceService;
        }
    }
}
