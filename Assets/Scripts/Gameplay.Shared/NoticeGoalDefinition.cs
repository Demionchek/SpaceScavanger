using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    [CreateAssetMenu(menuName = "Game/Quest/Goals/Notice", fileName = "NoticeGoal")]
    public sealed class NoticeGoalDefinition : QuestGoalDefinition
    {
        [Tooltip("Строка, которую панель заданий покажет вместо процентов.")]
        [SerializeField] private string _status = "Waiting";

        public override IQuestGoal CreateGoal(QuestDefinition quest, QuestGoalContext context)
        {
            return new NoticeGoal(_status);
        }
    }
}
