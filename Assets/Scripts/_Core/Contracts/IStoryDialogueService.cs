namespace Game.Core
{
    public interface IStoryDialogueService
    {
        bool HasPendingFor(StoryDialogueDelivery delivery);
        void Trigger(StoryTrigger source);

        // Звонок мимо сюжетного списка: тот одноразовый и последовательный,
        // а квота приходит снова и снова.
        void Enqueue(string node, StoryDialogueDelivery delivery, float delaySeconds = 0f);
        bool TryTakePending(StoryDialogueDelivery delivery, out string node);
    }
}
