using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    [CreateAssetMenu(menuName = "Game/Quest/Goals/Corporate Quota", fileName = "QuotaGoal")]
    public sealed class QuotaGoalDefinition : QuestGoalDefinition
    {
        [SerializeField] private ResourceType _resource;
        [SerializeField] private int _amount = 40;

        [Tooltip("Сколько секторов даётся на сдачу, считая тот, в котором квота выдана.")]
        [SerializeField] private int _deadlineSectors = 3;

        public ResourceType Resource => _resource;
        public int Amount => _amount;
        public int DeadlineSectors => _deadlineSectors;

        public override IQuestGoal CreateGoal(QuestDefinition quest, QuestGoalContext context)
        {
            return new QuotaGoal(quest, _resource, _amount, _deadlineSectors,
                context.ResourceService, context.SectorService);
        }
    }
}
