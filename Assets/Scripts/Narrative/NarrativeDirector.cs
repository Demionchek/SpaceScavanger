using Game.Core;
using Game.Data;
using UnityEngine;
using VContainer;
using Yarn.Unity;

namespace Game.Narrative
{
    public sealed class NarrativeDirector : MonoBehaviour
    {
        [SerializeField] private DialogueRunner _dialogueRunner;
        [SerializeField] private RandomEventCatalog _eventCatalog;
        [SerializeField] private ChoiceEffect[] _effects;

        private EventBus _eventBus;
        private GameContext _gameContext;
        private IPauseService _pauseService;
        private bool _dialogueActive;

        [Inject]
        public void Construct(EventBus eventBus, GameContext gameContext, IPauseService pauseService)
        {
            _eventBus = eventBus;
            _gameContext = gameContext;
            _pauseService = pauseService;
        }

        private void Awake()
        {
            _dialogueRunner.AddCommandHandler<string>("apply_effect", ApplyEffect);
            _dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);
        }

        private void OnEnable()
        {
            _eventBus.Subscribe<RandomEventRequestedEvent>(OnRandomEventRequested);
        }

        private void OnDisable()
        {
            _eventBus.Unsubscribe<RandomEventRequestedEvent>(OnRandomEventRequested);
            ReleasePauseIfActive();
        }

        private void OnRandomEventRequested(RandomEventRequestedEvent evt)
        {
            if (_dialogueActive || _eventCatalog == null || !_eventCatalog.HasEvents)
            {
                return;
            }

            _dialogueActive = true;
            _pauseService.RequestPause();
            _dialogueRunner.StartDialogue(_eventCatalog.GetRandomNode());
        }

        private void OnDialogueComplete()
        {
            ReleasePauseIfActive();
        }

        private void ReleasePauseIfActive()
        {
            if (!_dialogueActive)
            {
                return;
            }

            _dialogueActive = false;
            _pauseService.ReleasePause();
        }

        private void ApplyEffect(string effectName)
        {
            foreach (var effect in _effects)
            {
                if (effect != null && effect.name == effectName)
                {
                    effect.Apply(_gameContext);
                    return;
                }
            }

            Debug.LogWarning($"ChoiceEffect '{effectName}' not found in NarrativeDirector effects list");
        }
    }
}
