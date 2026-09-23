using System;
using VContainer.Unity;

namespace Game.Core
{
    // Единое правило видимости лётного HUD: не в интерьере, не в диалоге, не в
    // катсцене. Флаги раздельные: диалог внутри катсцены при завершении не должен
    // вернуть HUD, пока катсцена ещё идёт.
    public sealed class HudVisibility : IInitializable, IDisposable
    {
        private readonly EventBus _eventBus;

        private bool _inFlight;
        private bool _inDialogue;
        private bool _inCutscene;

        public HudVisibility(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        public bool IsVisible => _inFlight && !_inDialogue && !_inCutscene;

        // Initialize, а не Start: он гарантированно отрабатывает раньше любого
        // IStartable, в том числе бутстрапа, публикующего стартовое состояние.
        public void Initialize()
        {
            _eventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);
            _eventBus.Subscribe<DialogueStartedEvent>(OnDialogueStarted);
            _eventBus.Subscribe<DialogueFinishedEvent>(OnDialogueFinished);
            _eventBus.Subscribe<CutsceneStartedEvent>(OnCutsceneStarted);
            _eventBus.Subscribe<CutsceneFinishedEvent>(OnCutsceneFinished);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<GameStateChangedEvent>(OnStateChanged);
            _eventBus.Unsubscribe<DialogueStartedEvent>(OnDialogueStarted);
            _eventBus.Unsubscribe<DialogueFinishedEvent>(OnDialogueFinished);
            _eventBus.Unsubscribe<CutsceneStartedEvent>(OnCutsceneStarted);
            _eventBus.Unsubscribe<CutsceneFinishedEvent>(OnCutsceneFinished);
        }

        private void OnStateChanged(GameStateChangedEvent evt) => Set(ref _inFlight, evt.NewState is not ShipInteriorState);
        private void OnDialogueStarted(DialogueStartedEvent _) => Set(ref _inDialogue, true);
        private void OnDialogueFinished(DialogueFinishedEvent _) => Set(ref _inDialogue, false);
        private void OnCutsceneStarted(CutsceneStartedEvent _) => Set(ref _inCutscene, true);
        private void OnCutsceneFinished(CutsceneFinishedEvent _) => Set(ref _inCutscene, false);

        private void Set(ref bool flag, bool value)
        {
            var wasVisible = IsVisible;
            flag = value;

            if (wasVisible != IsVisible)
            {
                _eventBus.Publish(new HudVisibilityChangedEvent(IsVisible));
            }
        }
    }
}
