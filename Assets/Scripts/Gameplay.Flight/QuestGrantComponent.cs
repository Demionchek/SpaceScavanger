using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Flight
{
    // Выдаёт квест без NPC (обучение). _autoTurnIn закрывает его сразу по
    // выполнении — в туториале сдавать некому.
    public sealed class QuestGrantComponent : MonoBehaviour
    {
        [SerializeField] private QuestDefinition _quest;
        [SerializeField] private bool _autoTurnIn = true;

        private EventBus _eventBus;
        private IQuestService _questService;
        private GameContext _gameContext;

        [Inject]
        public void Construct(EventBus eventBus, IQuestService questService, GameContext gameContext)
        {
            _eventBus = eventBus;
            _questService = questService;
            _gameContext = gameContext;

            _eventBus.Subscribe<QuestCompletedEvent>(OnQuestCompleted);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<QuestCompletedEvent>(OnQuestCompleted);
        }

        private void Start()
        {
            _questService.StartQuest(_quest, _gameContext);
        }

        private void OnQuestCompleted(QuestCompletedEvent evt)
        {
            if (_autoTurnIn && evt.Quest == _quest)
            {
                _questService.TryTurnIn(_quest, _gameContext);
            }
        }
    }
}
