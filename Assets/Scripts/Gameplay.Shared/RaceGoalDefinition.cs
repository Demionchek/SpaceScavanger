using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    [CreateAssetMenu(menuName = "Game/Quest/Goals/Race", fileName = "RaceGoal")]
    public sealed class RaceGoalDefinition : QuestGoalDefinition
    {
        public override IQuestGoal CreateGoal(QuestDefinition quest, QuestGoalContext context)
        {
            return new RaceGoal(quest);
        }
    }
}
