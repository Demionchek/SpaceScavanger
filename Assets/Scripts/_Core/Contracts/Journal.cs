using System.Collections.Generic;

namespace Game.Core
{
    public enum JournalCategory
    {
        Quest,
        Reputation,
        Crafting,
        Travel
    }

    public readonly struct JournalEntry
    {
        public readonly JournalCategory Category;
        public readonly string Message;

        public JournalEntry(JournalCategory category, string message)
        {
            Category = category;
            Message = message;
        }
    }

    public readonly struct JournalChangedEvent
    {
    }

    public interface IJournalService
    {
        IReadOnlyList<JournalEntry> Entries { get; }
        void Add(JournalCategory category, string message);
    }
}
