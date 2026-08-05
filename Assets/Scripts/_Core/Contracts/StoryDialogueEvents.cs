namespace Game.Core
{
    public enum StoryDialogueDelivery
    {
        Immediate,
        Intercom,
        ShipComputer
    }

    // Публичный триггер: взять следующий диалог из сюжетного списка.
    public readonly struct StoryDialogueRequestedEvent
    {
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
