namespace Game.Core
{
    public enum StoryDialogueDelivery
    {
        Immediate,
        Intercom,
        ShipComputer
    }

    // Чем бит вызывается. Any — любым источником.
    public enum StoryTrigger
    {
        Any,
        TutorialComplete,
        WormholeJump,
        CutsceneEnd
    }

    // Публичный триггер: взять следующий диалог из сюжетного списка.
    public readonly struct StoryDialogueRequestedEvent
    {
        public readonly StoryTrigger Source;

        public StoryDialogueRequestedEvent(StoryTrigger source)
        {
            Source = source;
        }
    }

    // Диалог взят и ждёт игрока у своего канала (интерком звонит, терминал мигает).
    public readonly struct StoryDialoguePendingEvent
    {
        public readonly StoryDialogueDelivery Delivery;

        public StoryDialoguePendingEvent(StoryDialogueDelivery delivery)
        {
            Delivery = delivery;
        }
    }
}
