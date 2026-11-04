namespace Game.Core
{
    public sealed class GameContext
    {
        public IResourceService ResourceService { get; }
        public IQuestService QuestService { get; }
        public IReputationService ReputationService { get; }
        public IRelationshipService RelationshipService { get; }
        public ISectorService SectorService { get; }
        public EventBus EventBus { get; }

        public GameContext(
            IResourceService resourceService,
            IQuestService questService,
            IReputationService reputationService,
            IRelationshipService relationshipService,
            ISectorService sectorService,
            EventBus eventBus)
        {
            ResourceService = resourceService;
            QuestService = questService;
            ReputationService = reputationService;
            RelationshipService = relationshipService;
            SectorService = sectorService;
            EventBus = eventBus;
        }
    }
}
