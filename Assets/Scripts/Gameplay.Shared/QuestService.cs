using System.Collections.Generic;
using Game.Core;

namespace Game.Gameplay.Shared
{
    public sealed class QuestService : IQuestService
    {
        private readonly EventBus _eventBus;
        private readonly IResourceService _resources;
        private readonly List<QuestRuntime> _active = new();
        private readonly HashSet<QuestDefinition> _turnedIn = new();

        public QuestService(EventBus eventBus, IResourceService resources)
        {
            _eventBus = eventBus;
            _resources = resources;
        }

        public IReadOnlyList<QuestRuntime> ActiveQuests => _active;

        public QuestState GetState(QuestDefinition quest)
        {
            if (_turnedIn.Contains(quest))
            {
                return QuestState.TurnedIn;
            }

            return FindRuntime(quest) != null ? QuestState.Active : QuestState.NotStarted;
        }

        public bool StartQuest(QuestDefinition quest)
        {
            if (quest == null || quest.Goal == null || GetState(quest) != QuestState.NotStarted)
            {
                return false;
            }

            var goal = quest.Goal.CreateGoal(quest, new QuestGoalContext(_resources));
            goal.Bind(_eventBus);
            _active.Add(new QuestRuntime(quest, goal));
            _eventBus.Publish(new QuestStartedEvent(quest));
            return true;
        }

        public bool TryTurnIn(QuestDefinition quest, GameContext ctx)
        {
            var runtime = FindRuntime(quest);
            if (runtime == null || !runtime.Goal.IsComplete)
            {
                return false;
            }

            if (runtime.Goal is IDeliverableGoal deliverable && !deliverable.TryDeliver())
            {
                return false;
            }

            runtime.Goal.Unbind(_eventBus);
            _active.Remove(runtime);
            _turnedIn.Add(quest);

            foreach (var reward in quest.Rewards)
            {
                if (reward != null)
                {
                    reward.Apply(ctx);
                }
            }

            _eventBus.Publish(new QuestTurnedInEvent(quest));
            return true;
        }

        private QuestRuntime FindRuntime(QuestDefinition quest)
        {
            foreach (var runtime in _active)
            {
                if (runtime.Definition == quest)
                {
                    return runtime;
                }
            }

            return null;
        }
    }
}
