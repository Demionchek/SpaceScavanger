using Game.Core;

namespace Game.Gameplay.Shared
{
    // Коридор обучения зачищен — та же проверка, что открывает сюжетный бит,
    // поэтому гейт червоточины и сюжет больше не расходятся.
    public sealed class TutorialCorridorGoal : IQuestGoal, ISaveableGoal, IQuestGoalStatus
    {
        private readonly QuestDefinition _quest;

        private EventBus _bus;
        private bool _cleared;

        public TutorialCorridorGoal(QuestDefinition quest)
        {
            _quest = quest;
        }

        public bool IsComplete => _cleared;

        public float Progress => _cleared ? 1f : 0f;

        public string Status => "corridor not cleared";

        public void Bind(EventBus bus)
        {
            _bus = bus;
            bus.Subscribe<TutorialCorridorClearedEvent>(OnCleared);
        }

        public void Unbind(EventBus bus)
        {
            bus.Unsubscribe<TutorialCorridorClearedEvent>(OnCleared);
            _bus = null;
        }

        public string SaveProgress() => _cleared ? "1" : "0";

        public void LoadProgress(string data) => _cleared = data == "1";

        private void OnCleared(TutorialCorridorClearedEvent _)
        {
            if (_cleared)
            {
                return;
            }

            _cleared = true;
            _bus.Publish(new QuestCompletedEvent(_quest));
        }
    }
}
