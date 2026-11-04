using System;
using Game.Core;
using VContainer.Unity;

namespace Game.Gameplay.Flight
{
    // Обучающий сектор держит корабль закрытым: пока игрок не прыгнул, внутрь
    // ходить незачем. После прыжка замок снимается, а первый звонок объясняет,
    // зачем туда идти.
    public sealed class BoardingTutorial : IStartable, IDisposable
    {
        private const string CallHint =
            "A call is waiting on the ship intercom. Press E away from anything else to board your ship.";

        private readonly EventBus _eventBus;
        private readonly ModeLock _modeLock;
        private readonly ISectorService _sectors;
        private readonly GameStateMachine _stateMachine;

        private bool _locked;
        private bool _hintShown;

        public BoardingTutorial(EventBus eventBus, ModeLock modeLock, ISectorService sectors,
            GameStateMachine stateMachine)
        {
            _eventBus = eventBus;
            _modeLock = modeLock;
            _sectors = sectors;
            _stateMachine = stateMachine;
        }

        public void Start()
        {
            _eventBus.Subscribe<SectorChangedEvent>(OnSectorChanged);
            _eventBus.Subscribe<StoryDialoguePendingEvent>(OnCallPending);
            Apply(_sectors.Current);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe<SectorChangedEvent>(OnSectorChanged);
            _eventBus.Unsubscribe<StoryDialoguePendingEvent>(OnCallPending);
            Apply(2);
        }

        private void OnSectorChanged(SectorChangedEvent evt) => Apply(evt.Number);

        // Загрузка сохранения тоже публикует смену сектора, так что замок встаёт
        // и снимается по одному и тому же правилу.
        private void Apply(int sector)
        {
            var locked = sector <= 1;
            if (locked == _locked)
            {
                return;
            }

            _locked = locked;

            if (locked)
            {
                _modeLock.AddLock();
            }
            else
            {
                _modeLock.RemoveLock();
            }
        }

        private void OnCallPending(StoryDialoguePendingEvent evt)
        {
            // Первый звонок приходит, когда игрок и так в интерьере — там подсказка
            // про посадку бессмысленна, интерком и без неё мигает.
            if (_hintShown || evt.Delivery != StoryDialogueDelivery.Intercom ||
                _stateMachine.CurrentState is ShipInteriorState)
            {
                return;
            }

            _hintShown = true;
            _eventBus.Publish(new TutorialPopupEvent(CallHint, true));
        }
    }
}
