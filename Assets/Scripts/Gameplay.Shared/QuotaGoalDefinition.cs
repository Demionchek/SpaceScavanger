using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    [CreateAssetMenu(menuName = "Game/Quest/Goals/Corporate Quota", fileName = "QuotaGoal")]
    public sealed class QuotaGoalDefinition : QuestGoalDefinition
    {
        [SerializeField] private ResourceType _resource;

        [Tooltip("Квота в первом секторе.")]
        [SerializeField] private int _amount = 40;

        [Tooltip("Насколько квота тяжелеет за каждый сектор: в секторе N требуют Amount + (N-1) * это число.")]
        [SerializeField] private int _amountPerSector = 10;

        [Tooltip("Сколько секторов даётся на сдачу, считая тот, в котором квота выдана.")]
        [SerializeField] private int _deadlineSectors = 3;

        public override IQuestGoal CreateGoal(QuestDefinition quest, QuestGoalContext context)
        {
            return new QuotaGoal(quest, _resource, _amount, _amountPerSector, _deadlineSectors,
                context.ResourceService, context.SectorService);
        }
    }
}
