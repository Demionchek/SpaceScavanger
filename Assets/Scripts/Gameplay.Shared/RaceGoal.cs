using Game.Core;

namespace Game.Gameplay.Shared
{
    // Цель выполнена, если игрок финишировал не последним.
    public sealed class RaceGoal : IQuestGoal
    {
        private readonly QuestDefinition _quest;

        private EventBus _bus;
        private bool _complete;

        public RaceGoal(QuestDefinition quest)
        {
            _quest = quest;
        }

        public bool IsComplete => _complete;

        public float Progress => _complete ? 1f : 0f;

        public void Bind(EventBus bus)
        {
            _bus = bus;
            bus.Subscribe<RaceFinishedEvent>(OnRaceFinished);
        }

        public void Unbind(EventBus bus)
        {
            bus.Unsubscribe<RaceFinishedEvent>(OnRaceFinished);
            _bus = null;
        }

        private void OnRaceFinished(RaceFinishedEvent evt)
        {
            if (_complete || evt.Place >= evt.Participants)
            {
                return;
            }

            _complete = true;
            _bus.Publish(new QuestCompletedEvent(_quest));
        }
    }
}
