using UnityEngine;

namespace Game.Core
{
    public sealed class QuestGoalContext
    {
        public IResourceService ResourceService { get; }

        public QuestGoalContext(IResourceService resourceService)
        {
            ResourceService = resourceService;
        }
    }

    public abstract class QuestGoalDefinition : ScriptableObject
    {
        public abstract IQuestGoal CreateGoal(QuestDefinition quest, QuestGoalContext context);
    }
}
