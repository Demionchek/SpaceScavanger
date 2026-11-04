using Game.Core;
using UnityEngine;
using VContainer;

namespace Game.Gameplay.Flight
{
    // Звонок слышно и в космосе: интерком в интерьере звонит сам, но игрок чаще
    // всего в полёте, и до сих пор о вызове говорил только баннер.
    public sealed class IntercomCallSound : MonoBehaviour
    {
        [SerializeField] private AudioSource _source;

        private EventBus _eventBus;
        private IStoryDialogueService _storyDialogues;
        private GameStateMachine _stateMachine;

        [Inject]
        public void Construct(EventBus eventBus, IStoryDialogueService storyDialogues,
            GameStateMachine stateMachine)
        {
            _eventBus = eventBus;
            _storyDialogues = storyDialogues;
            _stateMachine = stateMachine;

            _eventBus.Subscribe<StoryDialoguePendingEvent>(OnCallChanged);
            _eventBus.Subscribe<DialogueFinishedEvent>(OnDialogueFinished);
            _eventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);
            _eventBus.Subscribe<GameLoadFinishedEvent>(OnLoadFinished);
        }

        private void OnDestroy()
        {
            if (_eventBus == null)
            {
                return;
            }

            _eventBus.Unsubscribe<StoryDialoguePendingEvent>(OnCallChanged);
            _eventBus.Unsubscribe<DialogueFinishedEvent>(OnDialogueFinished);
            _eventBus.Unsubscribe<GameStateChangedEvent>(OnStateChanged);
            _eventBus.Unsubscribe<GameLoadFinishedEvent>(OnLoadFinished);
        }

        private void Start() => Apply();

        private void OnCallChanged(StoryDialoguePendingEvent _) => Apply();

        private void OnDialogueFinished(DialogueFinishedEvent _) => Apply();

        private void OnStateChanged(GameStateChangedEvent _) => Apply();

        private void OnLoadFinished(GameLoadFinishedEvent _) => Apply();

        // В интерьере звонит сам интерком, дублировать его не нужно.
        private void Apply()
        {
            if (_source == null)
            {
                return;
            }

            var ringing = _storyDialogues.HasPendingFor(StoryDialogueDelivery.Intercom)
                          && _stateMachine.CurrentState is not ShipInteriorState;

            if (ringing && !_source.isPlaying)
            {
                _source.loop = true;
                _source.Play();
            }
            else if (!ringing && _source.isPlaying)
            {
                _source.Stop();
            }
        }
    }
}
