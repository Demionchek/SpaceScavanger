using System;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;

namespace Game.Gameplay.Shared
{
    public sealed class QuestService : IQuestService, ISaveable
    {
        private readonly EventBus _eventBus;
        private readonly IResourceService _resources;
        private readonly SaveAssetRegistry _registry;
        private readonly List<QuestRuntime> _active = new();
        private readonly HashSet<QuestDefinition> _turnedIn = new();

        public QuestService(EventBus eventBus, IResourceService resources, SaveAssetRegistry registry)
        {
            _eventBus = eventBus;
            _resources = resources;
            _registry = registry;
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

        public bool StartQuest(QuestDefinition quest, GameContext ctx)
        {
            if (quest == null || quest.Goal == null || GetState(quest) != QuestState.NotStarted)
            {
                return false;
            }

            var goal = quest.Goal.CreateGoal(quest, new QuestGoalContext(_resources));
            goal.Bind(_eventBus);
            _active.Add(new QuestRuntime(quest, goal));

            if (quest.OnStartEffects != null)
            {
                foreach (var effect in quest.OnStartEffects)
                {
                    if (effect != null)
                    {
                        effect.Apply(ctx);
                    }
                }
            }

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

        public string SaveId => "quests";

        public string Save()
        {
            var data = new SaveData();

            foreach (var quest in _turnedIn)
            {
                data.turnedIn.Add(_registry.GetId(quest));
            }

            foreach (var runtime in _active)
            {
                data.activeIds.Add(_registry.GetId(runtime.Definition));
                data.activeProgress.Add(runtime.Goal is ISaveableGoal goal ? goal.SaveProgress() : string.Empty);
            }

            return JsonUtility.ToJson(data);
        }

        public void Load(string json)
        {
            foreach (var runtime in _active)
            {
                runtime.Goal.Unbind(_eventBus);
            }

            _active.Clear();
            _turnedIn.Clear();

            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                return;
            }

            foreach (var id in data.turnedIn)
            {
                var quest = _registry.GetQuest(id);
                if (quest != null)
                {
                    _turnedIn.Add(quest);
                }
            }

            for (var i = 0; i < data.activeIds.Count; i++)
            {
                var quest = _registry.GetQuest(data.activeIds[i]);
                if (quest == null || quest.Goal == null)
                {
                    continue;
                }

                var goal = quest.Goal.CreateGoal(quest, new QuestGoalContext(_resources));
                goal.Bind(_eventBus);

                if (goal is ISaveableGoal saveable && !string.IsNullOrEmpty(data.activeProgress[i]))
                {
                    saveable.LoadProgress(data.activeProgress[i]);
                }

                _active.Add(new QuestRuntime(quest, goal));
            }
        }

        [Serializable]
        private sealed class SaveData
        {
            public List<string> turnedIn = new();
            public List<string> activeIds = new();
            public List<string> activeProgress = new();
        }
    }
}
