namespace Game.Core
{
    public sealed class GameContext
    {
        public IResourceService ResourceService { get; }
        public IQuestService QuestService { get; }

        public GameContext(IResourceService resourceService, IQuestService questService)
        {
            ResourceService = resourceService;
            QuestService = questService;
        }
    }
}
