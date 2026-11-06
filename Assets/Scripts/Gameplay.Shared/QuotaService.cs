using System;
using Game.Core;
using VContainer.Unity;

namespace Game.Gameplay.Shared
{
    // Квота корпорации: игрок сдаёт груз сам, просрочка стоит корабля,
    // а в следующем секторе приходит новая квота.
    public sealed class QuotaService : IQuotaService, IStartable, IDisposable
    {
        private const string FailureReason = "The Corporation collected on your sentence";
        private const string NextQuotaNode = "Story_10_Quota_Next";

        // Небольшая пауза, чтобы звонок не пришёлся на сам момент прибытия.
        private const float NextQuotaCallDelay = 15f;

        private readonly EventBus _eventBus;
        private readonly IQuestService _quests;
        private readonly IStoryDialogueService _storyDialogues;
        private readonly GameContext _context;
        private readonly QuestDefinition _quotaQuest;
        private readonly QuestDefinition _waitingQuest;

        public QuotaService(EventBus eventBus, IQuestService quests, IStoryDialogueService storyDialogues,
            GameContext context, QuestDefinition quotaQuest, QuestDefinition waitingQuest)
        {
            _eventBus = eventBus;
            _quests = quests;
            _storyDialogues = storyDialogues;
            _context = context;
            _quotaQuest = quotaQuest;
            _waitingQuest = waitingQuest;
        }

        public void Start()
        {
            _eventBus.Subscribe<SectorChangedEvent>(OnSectorChanged);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<SectorChangedEvent>(OnSectorChanged);
        }

        // Состояние квеста и есть память о сдаче, отдельный флаг не нужен —
        // иначе он бы терялся при загрузке.
        private void OnSectorChanged(SectorChangedEvent _)
        {
            if (_quotaQuest == null || _quests.GetState(_quotaQuest) != QuestState.TurnedIn)
            {
                return;
            }

            // Напоминание о следующей квоте дождалось прыжка и больше не нужно.
            _quests.Abandon(_waitingQuest);

            // Квест стартует сразу, а звонок только объясняет: иначе игрок, не снявший
            // трубку, остался бы без квоты и без срока.
            _quests.Reset(_quotaQuest);
            if (_quests.StartQuest(_quotaQuest, _context))
            {
                _storyDialogues.Enqueue(NextQuotaNode, StoryDialogueDelivery.Intercom, NextQuotaCallDelay);
            }
        }

        public bool CanDeliver
        {
            get
            {
                var goal = ActiveGoal();
                return goal != null && goal.IsComplete;
            }
        }

        public int Amount
        {
            get
            {
                var goal = ActiveGoal();
                return goal != null ? goal.Amount : 0;
            }
        }

        public int SectorsLeft
        {
            get
            {
                var goal = ActiveGoal();
                return goal != null ? Math.Max(0, goal.SectorsLeft) : 0;
            }
        }

        public bool TryDeliver()
        {
            if (_quotaQuest == null || !_quests.TryTurnIn(_quotaQuest, _context))
            {
                return false;
            }

            _eventBus.Publish(new NotificationRequestedEvent("Quota delivered"));

            // До прыжка корпорация молчит, поэтому в панели заданий остаётся строка,
            // объясняющая, когда ждать следующую квоту.
            if (_waitingQuest != null)
            {
                _quests.StartQuest(_waitingQuest, _context);
            }

            return true;
        }

        // Последний разрешённый сектор — тот, где остался один: прыжок из него уводит
        // за срок, поэтому груз забирают прямо здесь, а если его нет — взрывают корабль.
        public bool TryEnforceDeadline()
        {
            var goal = ActiveGoal();
            if (goal == null || goal.SectorsLeft > 1)
            {
                return false;
            }

            if (TryDeliver())
            {
                return false;
            }

            _eventBus.Publish(new PlayerDestroyedEvent(FailureReason));
            return true;
        }

        private QuotaGoal ActiveGoal()
        {
            if (_quotaQuest == null)
            {
                return null;
            }

            foreach (var runtime in _quests.ActiveQuests)
            {
                if (runtime.Definition == _quotaQuest)
                {
                    return runtime.Goal as QuotaGoal;
                }
            }

            return null;
        }
    }
}
