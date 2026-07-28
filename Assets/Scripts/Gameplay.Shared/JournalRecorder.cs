using System;
using Game.Core;
using VContainer.Unity;

namespace Game.Gameplay.Shared
{
    public sealed class JournalRecorder : IStartable, IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly IJournalService _journal;

        public JournalRecorder(EventBus eventBus, IJournalService journal)
        {
            _eventBus = eventBus;
            _journal = journal;
        }

        public void Start()
        {
            _eventBus.Subscribe<QuestStartedEvent>(OnQuestStarted);
            _eventBus.Subscribe<QuestCompletedEvent>(OnQuestCompleted);
            _eventBus.Subscribe<QuestTurnedInEvent>(OnQuestTurnedIn);
            _eventBus.Subscribe<ReputationChangedEvent>(OnReputationChanged);
            _eventBus.Subscribe<ItemCraftedEvent>(OnItemCrafted);
            _eventBus.Subscribe<WormholeTravelRequestedEvent>(OnTravel);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<QuestStartedEvent>(OnQuestStarted);
            _eventBus.Unsubscribe<QuestCompletedEvent>(OnQuestCompleted);
            _eventBus.Unsubscribe<QuestTurnedInEvent>(OnQuestTurnedIn);
            _eventBus.Unsubscribe<ReputationChangedEvent>(OnReputationChanged);
            _eventBus.Unsubscribe<ItemCraftedEvent>(OnItemCrafted);
            _eventBus.Unsubscribe<WormholeTravelRequestedEvent>(OnTravel);
        }

        private void OnQuestStarted(QuestStartedEvent e) =>
            _journal.Add(JournalCategory.Quest, $"Quest started: {e.Quest.Title}");

        private void OnQuestCompleted(QuestCompletedEvent e) =>
            _journal.Add(JournalCategory.Quest, $"Quest completed: {e.Quest.Title}");

        private void OnQuestTurnedIn(QuestTurnedInEvent e) =>
            _journal.Add(JournalCategory.Quest, $"Quest turned in: {e.Quest.Title}");

        private void OnReputationChanged(ReputationChangedEvent e) =>
            _journal.Add(JournalCategory.Reputation, $"Reputation — {e.Group}: {e.NewValue}");

        private void OnItemCrafted(ItemCraftedEvent e) =>
            _journal.Add(JournalCategory.Crafting, $"Crafted: {e.Item.DisplayName}");

        private void OnTravel(WormholeTravelRequestedEvent e) =>
            _journal.Add(JournalCategory.Travel, "Traveled through a wormhole");
    }
}
