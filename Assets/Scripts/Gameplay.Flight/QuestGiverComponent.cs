using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Flight
{
    public sealed class QuestGiverComponent : MonoBehaviour, IInteractable
    {
        [SerializeField] private QuestDefinition _quest;
        [SerializeField] private string _offerDialogueNode;
        [SerializeField] private string _turnInDialogueNode;
        [SerializeField] private string _prompt = "Quest";

        private IQuestService _questService;
        private GameContext _gameContext;
        private EventBus _eventBus;

        [Inject]
        public void Construct(IQuestService questService, GameContext gameContext, EventBus eventBus)
        {
            _questService = questService;
            _gameContext = gameContext;
            _eventBus = eventBus;
        }

        public string Prompt => _prompt;

        public void Interact(PlayerContext ctx)
        {
            switch (_questService.GetState(_quest))
            {
                case QuestState.NotStarted:
                    if (!string.IsNullOrEmpty(_offerDialogueNode))
                    {
                        _eventBus.Publish(new DialogueRequestedEvent(_offerDialogueNode));
                    }
                    else
                    {
                        _questService.StartQuest(_quest, _gameContext);
                    }
                    break;

                case QuestState.Active:
                    if (!string.IsNullOrEmpty(_turnInDialogueNode))
                    {
                        _eventBus.Publish(new DialogueRequestedEvent(_turnInDialogueNode));
                    }
                    else if (!_questService.TryTurnIn(_quest, _gameContext))
                    {
                        Debug.Log($"Quest '{_quest.Title}' is not complete yet");
                    }
                    break;
            }
        }
    }
}
