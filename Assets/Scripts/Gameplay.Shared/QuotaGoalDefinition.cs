using System;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    // Одна строка квоты. Глубина сектора добавляет строки, а не только тонны:
    // с какого-то сектора корпорация начинает требовать железо, потом золото.
    [Serializable]
    public sealed class QuotaDemand
    {
        public ResourceType Resource;

        [Tooltip("С какого сектора корпорация начинает требовать этот ресурс.")]
        public int FromSector = 1;

        [Tooltip("Сколько требуют в секторе FromSector.")]
        public int Amount = 40;

        [Tooltip("Насколько требование тяжелеет за каждый сектор глубже FromSector.")]
        public int AmountPerSector;

        public bool AppliesTo(int sector) => sector >= FromSector && Amount > 0;

        public int AmountFor(int sector) =>
            Mathf.Max(1, Amount + AmountPerSector * (sector - FromSector));
    }

    [CreateAssetMenu(menuName = "Game/Quest/Goals/Corporate Quota", fileName = "QuotaGoal")]
    public sealed class QuotaGoalDefinition : QuestGoalDefinition
    {
        [Tooltip("Строки квоты: по одной на ресурс.")]
        [SerializeField] private QuotaDemand[] _demands;

        [Tooltip("Сколько секторов даётся на сдачу, считая тот, в котором квота выдана.")]
        [SerializeField] private int _deadlineSectors = 3;

        public override IQuestGoal CreateGoal(QuestDefinition quest, QuestGoalContext context)
        {
            return new QuotaGoal(quest, _demands, _deadlineSectors,
                context.ResourceService, context.SectorService);
        }
    }
}
