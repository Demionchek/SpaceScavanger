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
            _eventBus.Subscribe<DialogueRequestedEvent>(OnDialogueRequested);
        }

        private void OnDisable()
        {
            _eventBus.Unsubscribe<RandomEventRequestedEvent>(OnRandomEventRequested);
            _eventBus.Unsubscribe<DialogueRequestedEvent>(OnDialogueRequested);
            ReleasePauseIfActive();
        }

        private void OnRandomEventRequested(RandomEventRequestedEvent evt)
        {
            if (_eventCatalog == null || !_eventCatalog.HasEvents)
            {
                return;
            }

            StartDialogue(_eventCatalog.GetRandomNode());
        }

        private void OnDialogueRequested(DialogueRequestedEvent evt)
        {
            StartDialogue(evt.Node);
        }

        private void StartDialogue(string node)
        {
            if (_dialogueActive || string.IsNullOrEmpty(node))
            {
                return;
            }

            if (_dialogueRunner.YarnProject == null || !_dialogueRunner.YarnProject.Program.Nodes.ContainsKey(node))
            {
                Debug.LogError($"Yarn node '{node}' not found — dialogue not started");
                return;
            }

            _dialogueActive = true;
            _pauseService.RequestPause();
            _dialogueRunner.StartDialogue(node);
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
