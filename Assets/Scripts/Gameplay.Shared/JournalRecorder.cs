using System;
using Game.Core;
using VContainer.Unity;

namespace Game.Gameplay.Shared
{
    public sealed class JournalRecorder : IStartable, IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly IJournalService _journal;
        private bool _loading;

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
            _eventBus.Subscribe<GameLoadStartedEvent>(OnLoadStarted);
            _eventBus.Subscribe<GameLoadFinishedEvent>(OnLoadFinished);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<QuestStartedEvent>(OnQuestStarted);
            _eventBus.Unsubscribe<QuestCompletedEvent>(OnQuestCompleted);
            _eventBus.Unsubscribe<QuestTurnedInEvent>(OnQuestTurnedIn);
            _eventBus.Unsubscribe<ReputationChangedEvent>(OnReputationChanged);
            _eventBus.Unsubscribe<ItemCraftedEvent>(OnItemCrafted);
            _eventBus.Unsubscribe<WormholeTravelRequestedEvent>(OnTravel);
            _eventBus.Unsubscribe<GameLoadStartedEvent>(OnLoadStarted);
            _eventBus.Unsubscribe<GameLoadFinishedEvent>(OnLoadFinished);
        }

        private void OnLoadStarted(GameLoadStartedEvent _) => _loading = true;
        private void OnLoadFinished(GameLoadFinishedEvent _) => _loading = false;

        private void Log(JournalCategory category, string message)
        {
            if (!_loading)
            {
                _journal.Add(category, message);
            }
        }

        private void OnQuestStarted(QuestStartedEvent e) =>
            Log(JournalCategory.Quest, $"Quest started: {e.Quest.Title}");

        private void OnQuestCompleted(QuestCompletedEvent e) =>
            Log(JournalCategory.Quest, $"Quest completed: {e.Quest.Title}");

        private void OnQuestTurnedIn(QuestTurnedInEvent e) =>
            Log(JournalCategory.Quest, $"Quest turned in: {e.Quest.Title}");

        private void OnReputationChanged(ReputationChangedEvent e) =>
            Log(JournalCategory.Reputation, $"Reputation — {e.Group}: {e.NewValue}");

        private void OnItemCrafted(ItemCraftedEvent e) =>
            Log(JournalCategory.Crafting, $"Crafted: {e.Item.DisplayName}");

        private void OnTravel(WormholeTravelRequestedEvent e) =>
            Log(JournalCategory.Travel, "Traveled through a wormhole");
    }
}
