using Game.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using VContainer;

namespace Game.Gameplay.Ship
{
    public sealed class IntercomComponent : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _prompt = "Answer intercom";

        [Header("Сдача квоты")]
        [SerializeField] private string _quotaPrompt = "Deliver quota";

        [TextArea]
        [SerializeField] private string _quotaConfirmMessage =
            "A Corporation tender is standing by on the channel. Hand the quota over?";

        [SerializeField] private string _quotaConfirmLabel = "Deliver";
        [SerializeField] private string _quotaCancelLabel = "Not yet";
        [SerializeField] private Light2D _light;
        [SerializeField] private AudioSource _callAudioSource;
        [SerializeField] private float _blinkInterval = 0.5f;

        private EventBus _eventBus;
        private IStoryDialogueService _storyDialogues;
        private IQuotaService _quotaService;
        private State _state;
        private string _activeNode;
        private float _nextBlinkTime;

        [Inject]
        public void Construct(EventBus eventBus, IStoryDialogueService storyDialogues,
            IQuotaService quotaService)
        {
            _eventBus = eventBus;
            _storyDialogues = storyDialogues;
            _quotaService = quotaService;

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
            // Набранный груз держит лампу ровным светом: мигание означает звонок.
            if (_state == State.Idle)
            {
                if (_light != null)
                {
                    _light.enabled = CanDeliverQuota;
                }

                return;
            }

            if (_state != State.Calling || _light == null || Time.unscaledTime < _nextBlinkTime)
            {
                return;
            }

            _nextBlinkTime = Time.unscaledTime + _blinkInterval;
            _light.enabled = !_light.enabled;
        }

        // Звонок важнее: груз можно сдать и после разговора.
        public string Prompt => _state == State.Calling || !CanDeliverQuota ? _prompt : _quotaPrompt;

        private bool CanDeliverQuota => _quotaService != null && _quotaService.CanDeliver;

        public void Interact(PlayerContext ctx)
        {
            if (_state != State.Calling)
            {
                if (CanDeliverQuota)
                {
                    ctx.EventBus.Publish(new ConfirmRequestedEvent(_quotaConfirmMessage,
                        () => _quotaService.TryDeliver(), _quotaConfirmLabel, _quotaCancelLabel));
                }

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
            if (_state != State.Talking || evt.Node != _activeNode)
            {
                return;
            }

            _activeNode = null;

            // Звонков в очереди может быть несколько — тогда интерком звонит снова.
            SetState(_storyDialogues.HasPendingFor(StoryDialogueDelivery.Intercom)
                ? State.Calling
                : State.Idle);
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
