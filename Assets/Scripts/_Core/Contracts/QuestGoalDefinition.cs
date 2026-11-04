using UnityEngine;

namespace Game.Core
{
    public sealed class QuestGoalContext
    {
        public IResourceService ResourceService { get; }
        public ISectorService SectorService { get; }

        public QuestGoalContext(IResourceService resourceService, ISectorService sectorService)
        {
            ResourceService = resourceService;
            SectorService = sectorService;
        }
    }

    public abstract class QuestGoalDefinition : ScriptableObject
    {
        public abstract IQuestGoal CreateGoal(QuestDefinition quest, QuestGoalContext context);
    }
}
