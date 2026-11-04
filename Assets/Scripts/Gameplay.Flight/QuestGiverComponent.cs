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

        [Inject]
        public void Construct(IQuestService questService, ISectorService sectors, GameContext gameContext,
            EventBus eventBus)
        {
            _questService = questService;
            _sectors = sectors;
            _gameContext = gameContext;
            _eventBus = eventBus;
        }

        public string Prompt => _prompt;

        private QuestDefinition Quest => _resolved != null ? _resolved : _resolved = Resolve();

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
        // предложил бы второе и не принял бы первое.
        private QuestDefinition Resolve()
        {
            if (_quest != null || _questPool == null)
            {
                return _quest;
            }

            QuestDefinition fresh = null;
            var seen = 0;

            foreach (var quest in _questPool.ForSector(_sectors.Current))
            {
                var state = _questService.GetState(quest);
                if (state == QuestState.Active)
                {
                    return quest;
                }

                if (state != QuestState.NotStarted)
                {
                    continue;
                }

                seen++;
                if (Random.Range(0, seen) == 0)
                {
                    fresh = quest;
                }
            }

            return fresh;
        }
    }
}
