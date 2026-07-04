namespace Game.Core
{
    public sealed class HookContext
    {
        public IResourceService ResourceService { get; }

        public HookContext(IResourceService resourceService)
        {
            ResourceService = resourceService;
        }
    }
}
