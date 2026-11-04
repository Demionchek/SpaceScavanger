using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    [CreateAssetMenu(menuName = "Game/Quest/Goals/Tutorial Corridor", fileName = "TutorialCorridorGoal")]
    public sealed class TutorialCorridorGoalDefinition : QuestGoalDefinition
    {
        public override IQuestGoal CreateGoal(QuestDefinition quest, QuestGoalContext context)
        {
            return new TutorialCorridorGoal(quest);
        }
    }
}
