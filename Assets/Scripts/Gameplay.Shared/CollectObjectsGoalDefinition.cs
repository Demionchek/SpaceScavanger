using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    [CreateAssetMenu(menuName = "Game/Quest/Goals/Collect Objects", fileName = "CollectObjectsGoal")]
    public sealed class CollectObjectsGoalDefinition : QuestGoalDefinition
    {
        [SerializeField] private int _count = 1;

        public override IQuestGoal CreateGoal(QuestDefinition quest, QuestGoalContext context)
        {
            return new CollectObjectsGoal(quest, _count);
        }
    }
}
