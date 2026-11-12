using Game.Core;
using Game.Data;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

namespace Game.Gameplay.Flight
{
    public sealed class QuestGiverComponent : MonoBehaviour, IInteractable
    {
        [Tooltip("Жёстко заданное задание. Пусто — берётся из пула по номеру сектора.")]
        [SerializeField] private QuestDefinition _quest;

        [SerializeField] private QuestPoolConfig _questPool;
        [SerializeField] private string _offerDialogueNode;
        [SerializeField] private string _turnInDialogueNode;
        [SerializeField] private string _prompt = "Quest";

        private IQuestService _questService;
        private ISectorService _sectors;
        private GameContext _gameContext;
        private EventBus _eventBus;
        private QuestDefinition _resolved;
        private bool _resolveTried;

        [Inject]
        public void Construct(IQuestService questService, ISectorService sectors, GameContext gameContext,
            EventBus eventBus)
        {
            _questService = questService;
            _sectors = sectors;
            _gameContext = gameContext;
            _eventBus = eventBus;
        }

        // Подпись — название самого задания, а не имя префаба: один и тот же
        // квестодатель берёт из пула что угодно, и "Quest_Pirates" над перевозчиком
        // груза просто врал.
        public string Prompt
        {
            get
            {
                var quest = Quest;
                return quest != null && !string.IsNullOrEmpty(quest.Title) ? quest.Title : _prompt;
            }
        }

        // Подсказку спрашивают десять раз в секунду, а Resolve обходит всю сцену:
        // пустой результат запоминается так же, как найденный.
        private QuestDefinition Quest
        {
            get
            {
                if (_resolved != null || _resolveTried)
                {
                    return _resolved;
                }

                _resolveTried = true;
                return _resolved = Resolve();
            }
        }

        public void Interact(PlayerContext ctx)
        {
            var quest = Quest;
            if (quest == null)
            {
                return;
            }

            switch (_questService.GetState(quest))
            {
                case QuestState.NotStarted:
                    var offer = Node(quest.OfferNode, _offerDialogueNode);
                    if (!string.IsNullOrEmpty(offer))
                    {
                        _eventBus.Publish(new DialogueRequestedEvent(offer));
                    }
                    else
                    {
                        _questService.StartQuest(quest, _gameContext);
                    }
                    break;

                case QuestState.Active:
                    var turnIn = Node(quest.TurnInNode, _turnInDialogueNode);
                    if (!string.IsNullOrEmpty(turnIn))
                    {
                        _eventBus.Publish(new DialogueRequestedEvent(turnIn));
                    }
                    else if (!_questService.TryTurnIn(quest, _gameContext))
                    {
                        Debug.Log($"Quest '{quest.Title}' is not complete yet");
                    }
                    break;
            }
        }

        private static string Node(string questNode, string fallback) =>
            string.IsNullOrEmpty(questNode) ? fallback : questNode;

        // Уже начатое задание важнее нового: иначе после загрузки этот же NPC
        // предложил бы второе и не принял бы первое. Заказ, который уже выбрал
        // сосед по сектору, не в счёт — квестодателей здесь несколько.
        private QuestDefinition Resolve()
        {
            if (_quest != null || _questPool == null)
            {
                return _quest;
            }

            var others = FindObjectsByType<QuestGiverComponent>(FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            QuestDefinition fresh = null;
            QuestDefinition repeat = null;
            var freshSeen = 0;
            var repeatSeen = 0;

            foreach (var quest in _questPool.ForSector(_sectors.Current))
            {
                if (TakenByAnother(others, quest))
                {
                    continue;
                }

                switch (_questService.GetState(quest))
                {
                    case QuestState.Active:
                        return quest;

                    case QuestState.NotStarted:
                        freshSeen++;
                        if (Random.Range(0, freshSeen) == 0)
                        {
                            fresh = quest;
                        }

                        break;

                    case QuestState.TurnedIn when quest.Repeatable:
                        repeatSeen++;
                        if (Random.Range(0, repeatSeen) == 0)
                        {
                            repeat = quest;
                        }

                        break;
                }
            }

            if (fresh != null)
            {
                return fresh;
            }

            // Новых заказов в секторе не осталось: повторяемый возвращается в пул,
            // иначе квестодатель стоял бы молча.
            if (repeat != null)
            {
                _questService.Reset(repeat);
            }

            return repeat;
        }

        private bool TakenByAnother(QuestGiverComponent[] others, QuestDefinition quest)
        {
            foreach (var other in others)
            {
                if (other != this && other._resolved == quest)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
