using System.Collections.Generic;
using Game.Core;

namespace Game.Gameplay.Shared
{
    public sealed class JournalService : IJournalService
    {
        private readonly EventBus _eventBus;
        private readonly List<JournalEntry> _entries = new();

        public JournalService(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public IReadOnlyList<JournalEntry> Entries => _entries;

        public void Add(JournalCategory category, string message)
        {
            _entries.Add(new JournalEntry(category, message));
            _eventBus.Publish(new JournalChangedEvent());
        }
    }
}
