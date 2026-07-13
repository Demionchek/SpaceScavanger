using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    [CreateAssetMenu(menuName = "Game/Quest/Goals/Collect Resource", fileName = "CollectResourceGoal")]
    public sealed class CollectResourceGoalDefinition : QuestGoalDefinition
    {
        [SerializeField] private ResourceType _resource;
        [SerializeField] private int _amount = 1;

        public override IQuestGoal CreateGoal(QuestDefinition quest, QuestGoalContext context)
        {
            return new CollectAndDeliverGoal(quest, _resource, _amount, context.ResourceService);
        }
    }
}
