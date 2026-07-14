using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    [CreateAssetMenu(menuName = "Game/Quest/Goals/Craft Item", fileName = "CraftItemGoal")]
    public sealed class CraftItemGoalDefinition : QuestGoalDefinition
    {
        [SerializeField] private ItemDefinition _item;
        [SerializeField] private int _count = 1;

        public override IQuestGoal CreateGoal(QuestDefinition quest, QuestGoalContext context)
        {
            return new CraftItemGoal(quest, _item, _count);
        }
    }
}
