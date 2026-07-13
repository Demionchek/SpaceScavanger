namespace Game.Core
{
    public readonly struct QuestStartedEvent
    {
        public readonly QuestDefinition Quest;

        public QuestStartedEvent(QuestDefinition quest)
        {
            Quest = quest;
        }
    }

    public readonly struct QuestCompletedEvent
    {
        public readonly QuestDefinition Quest;

        public QuestCompletedEvent(QuestDefinition quest)
        {
            Quest = quest;
        }
    }

    public readonly struct QuestTurnedInEvent
    {
        public readonly QuestDefinition Quest;

        public QuestTurnedInEvent(QuestDefinition quest)
        {
            Quest = quest;
        }
    }
}
