using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(menuName = "Game/Quest/Quest Definition", fileName = "QuestDefinition")]
    public sealed class QuestDefinition : ScriptableObject
    {
        public string Title;
        [TextArea] public string Description;
        public QuestGoalDefinition Goal;

        [Tooltip("Ноды выдачи и сдачи. Пусто — берутся те, что заданы на самом NPC.")]
        public string OfferNode;
        public string TurnInNode;
        public ChoiceEffect[] OnStartEffects;
        public ChoiceEffect[] Rewards;
    }
}
