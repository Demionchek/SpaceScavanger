using System.Collections.Generic;

namespace Game.Core
{
    public enum QuestState
    {
        NotStarted,
        Active,
        TurnedIn
    }

    public interface IQuestService
    {
        IReadOnlyList<QuestRuntime> ActiveQuests { get; }
        QuestState GetState(QuestDefinition quest);
        bool StartQuest(QuestDefinition quest, GameContext ctx);
        bool TryTurnIn(QuestDefinition quest, GameContext ctx);
    }
}
