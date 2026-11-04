using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [Serializable]
    public sealed class QuestPoolTier
    {
        [Tooltip("С какого сектора эти задания начинают попадаться.")]
        public int FromSector = 1;

        public QuestDefinition[] Quests;
    }

    [CreateAssetMenu(menuName = "Game/Quest/Quest Pool", fileName = "QuestPool")]
    public sealed class QuestPoolConfig : ScriptableObject
    {
        [SerializeField] private QuestPoolTier[] _tiers;

        // Доступно всё, до чего игрок дорос: дальние секторы не отменяют ранние задания.
        public IEnumerable<QuestDefinition> ForSector(int sector)
        {
            if (_tiers == null)
            {
                yield break;
            }

            foreach (var tier in _tiers)
            {
                if (tier?.Quests == null || tier.FromSector > sector)
                {
                    continue;
                }

                foreach (var quest in tier.Quests)
                {
                    if (quest != null)
                    {
                        yield return quest;
                    }
                }
            }
        }
    }
}
