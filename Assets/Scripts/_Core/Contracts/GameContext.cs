namespace Game.Core
{
    public sealed class GameContext
    {
        public IResourceService ResourceService { get; }
        public IQuestService QuestService { get; }
        public IReputationService ReputationService { get; }
        public EventBus EventBus { get; }

        public GameContext(
            IResourceService resourceService,
            IQuestService questService,
            IReputationService reputationService,
            EventBus eventBus)
        {
            ResourceService = resourceService;
            QuestService = questService;
            ReputationService = reputationService;
            EventBus = eventBus;
        }
    }
}
