using Game.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using VContainer;

namespace Game.Gameplay.Ship
{
    public sealed class IntercomComponent : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _prompt = "Answer intercom";
        [SerializeField] private Light2D _light;
        [SerializeField] private AudioSource _callAudioSource;
        [SerializeField] private float _blinkInterval = 0.5f;

        private EventBus _eventBus;
        private IStoryDialogueService _storyDialogues;
        private State _state;
        private string _activeNode;
        private float _nextBlinkTime;

        [Inject]
        public void Construct(EventBus eventBus, IStoryDialogueService storyDialogues)
        {
            _eventBus = eventBus;
            _storyDialogues = storyDialogues;

            _eventBus.Subscribe<StoryDialoguePendingEvent>(OnCallIncoming);
            _eventBus.Subscribe<DialogueFinishedEvent>(OnDialogueFinished);
        }

        private void OnDestroy()
        {
            if (_eventBus == null)
            {
                return;
            }

            _eventBus.Unsubscribe<StoryDialoguePendingEvent>(OnCallIncoming);
            _eventBus.Unsubscribe<DialogueFinishedEvent>(OnDialogueFinished);
        }

        // Сцена интерьера грузится additive, поэтому вызов мог прийти до её загрузки —
        // при появлении интеркома состояние восстанавливается из сервиса.
        private void Start()
        {
            SetState(_storyDialogues.HasPendingFor(StoryDialogueDelivery.Intercom) ? State.Calling : State.Idle);
        }

        private void Update()
        {
            if (_state != State.Calling || _light == null || Time.unscaledTime < _nextBlinkTime)
            {
                return;
            }

            _nextBlinkTime = Time.unscaledTime + _blinkInterval;
            _light.enabled = !_light.enabled;
        }

        public string Prompt => _prompt;

        public void Interact(PlayerContext ctx)
        {
            if (_state != State.Calling)
            {
                return;
            }

            if (!_storyDialogues.TryTakePending(StoryDialogueDelivery.Intercom, out var node))
            {
                SetState(State.Idle);
                return;
            }

            _activeNode = node;
            SetState(State.Talking);
            _eventBus.Publish(new DialogueRequestedEvent(node));
        }

        private void OnCallIncoming(StoryDialoguePendingEvent evt)
        {
            if (evt.Delivery == StoryDialogueDelivery.Intercom && _state == State.Idle)
            {
                SetState(State.Calling);
            }
        }

        private void OnDialogueFinished(DialogueFinishedEvent evt)
        {
            if (_state == State.Talking && evt.Node == _activeNode)
            {
                _activeNode = null;
                SetState(State.Idle);
            }
        }

        private void SetState(State state)
        {
            _state = state;

            if (_light != null)
            {
                _light.enabled = state != State.Idle;
            }

            if (_callAudioSource == null)
            {
                return;
            }

            if (state == State.Calling)
            {
                _callAudioSource.loop = true;
                _callAudioSource.Play();
            }
            else
            {
                _callAudioSource.Stop();
            }
        }

        private enum State
        {
            Idle,
            Calling,
            Talking
        }
    }
}
