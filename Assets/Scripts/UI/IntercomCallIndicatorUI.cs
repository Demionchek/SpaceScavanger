using DG.Tweening;
using Game.Core;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.UI
{
    public sealed class IntercomCallIndicatorUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private float _idleAlpha;
        [SerializeField] private float _minAlpha = 0.2f;
        [SerializeField] private float _maxAlpha = 1f;
        [SerializeField] private float _pulseDuration = 0.8f;
        [SerializeField] private Ease _pulseEase = Ease.InOutSine;

        private EventBus _eventBus;
        private IStoryDialogueService _storyDialogues;
        private Tween _pulse;
        private bool _hasCall;
        private bool _hudVisible = true;

        [Inject]
        public void Construct(EventBus eventBus, IStoryDialogueService storyDialogues, HudVisibility hud)
        {
            _eventBus = eventBus;
            _storyDialogues = storyDialogues;
            _hudVisible = hud.IsVisible;

            _eventBus.Subscribe<StoryDialoguePendingEvent>(OnCallIncoming);
            _eventBus.Subscribe<DialogueStartedEvent>(OnDialogueStarted);
            _eventBus.Subscribe<HudVisibilityChangedEvent>(OnHudVisibilityChanged);
            _eventBus.Subscribe<GameLoadFinishedEvent>(OnLoadFinished);
        }

        private void OnDestroy()
        {
            _pulse?.Kill();

            if (_eventBus == null)
            {
                return;
            }

            _eventBus.Unsubscribe<StoryDialoguePendingEvent>(OnCallIncoming);
            _eventBus.Unsubscribe<DialogueStartedEvent>(OnDialogueStarted);
            _eventBus.Unsubscribe<HudVisibilityChangedEvent>(OnHudVisibilityChanged);
            _eventBus.Unsubscribe<GameLoadFinishedEvent>(OnLoadFinished);
        }

        // Start, а не Construct: к этому моменту контейнер собран и оба флага актуальны.
        private void Start()
        {
            _hasCall = HasCall;
            Apply();
        }

        private void OnEnable()
        {
            Apply();
        }

        private void OnDisable()
        {
            _pulse?.Kill();
            _pulse = null;
        }

        private bool HasCall => _storyDialogues != null &&
                                _storyDialogues.HasPendingFor(StoryDialogueDelivery.Intercom);

        private void OnCallIncoming(StoryDialoguePendingEvent evt)
        {
            if (evt.Delivery == StoryDialogueDelivery.Intercom)
            {
                SetHasCall(true);
            }
        }

        // Звонок забирается в момент старта диалога — отдельного события на это нет.
        private void OnDialogueStarted(DialogueStartedEvent _) => SetHasCall(HasCall);

        private void OnLoadFinished(GameLoadFinishedEvent _) => SetHasCall(HasCall);

        // Иконка — часть лётного HUD, поэтому живёт по общему правилу: не в интерьере,
        // не в диалоге, не в катсцене. Не полагаемся на список корней в сцене.
        private void OnHudVisibilityChanged(HudVisibilityChangedEvent evt)
        {
            if (_hudVisible == evt.Visible)
            {
                return;
            }

            _hudVisible = evt.Visible;
            Apply();
        }

        private void SetHasCall(bool hasCall)
        {
            if (_hasCall == hasCall)
            {
                return;
            }

            _hasCall = hasCall;
            Apply();
        }

        private void Apply()
        {
            if (_icon == null)
            {
                return;
            }

            _pulse?.Kill();
            _pulse = null;

            _icon.enabled = _hudVisible && (_hasCall || _idleAlpha > 0f);

            if (!_hasCall || !_hudVisible)
            {
                SetAlpha(_idleAlpha);
                return;
            }

            SetAlpha(_maxAlpha);

            // Диалог ставит игру на паузу, поэтому пульс идёт в нескейленном времени.
            if (isActiveAndEnabled)
            {
                _pulse = DOTween.To(() => _icon.color.a, SetAlpha, _minAlpha, _pulseDuration)
                                .SetEase(_pulseEase)
                                .SetLoops(-1, LoopType.Yoyo)
                                .SetUpdate(true);
            }
        }

        private void SetAlpha(float alpha)
        {
            var color = _icon.color;
            color.a = alpha;
            _icon.color = color;
        }
    }
}
