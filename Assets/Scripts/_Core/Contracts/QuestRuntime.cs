namespace Game.Core
{
    public sealed class QuestRuntime
    {
        public QuestDefinition Definition { get; }
        public IQuestGoal Goal { get; }

        public QuestRuntime(QuestDefinition definition, IQuestGoal goal)
        {
            Definition = definition;
            Goal = goal;
        }
    }
}
