using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    [CreateAssetMenu(menuName = "Game/Quest/Goals/Destroy Group", fileName = "DestroyGroupGoal")]
    public sealed class DestroyGroupGoalDefinition : QuestGoalDefinition
    {
        [SerializeField] private string _groupTag;
        [SerializeField] private int _count = 1;

        public override IQuestGoal CreateGoal(QuestDefinition quest, QuestGoalContext context)
        {
            return new DestroyGroupGoal(quest, _groupTag, _count);
        }
    }
}
