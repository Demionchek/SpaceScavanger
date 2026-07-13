using Game.Core;
using UnityEngine;

namespace Game.Data
{
    [CreateAssetMenu(menuName = "Game/Effects/Start Quest", fileName = "StartQuestEffect")]
    public sealed class StartQuestEffect : ChoiceEffect
    {
        [SerializeField] private QuestDefinition _quest;

        public override void Apply(GameContext ctx)
        {
            ctx.QuestService.StartQuest(_quest);
        }
    }
}
