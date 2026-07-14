using UnityEngine;

namespace Game.Core
{
    [CreateAssetMenu(menuName = "Game/Quest/Quest Definition", fileName = "QuestDefinition")]
    public sealed class QuestDefinition : ScriptableObject
    {
        public string Title;
        [TextArea] public string Description;
        public QuestGoalDefinition Goal;
        public ChoiceEffect[] OnStartEffects;
        public ChoiceEffect[] Rewards;
    }
}
