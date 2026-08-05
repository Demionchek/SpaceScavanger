namespace Game.Core
{
    public interface IStoryDialogueService
    {
        bool HasPendingFor(StoryDialogueDelivery delivery);
        void Trigger();
        bool TryTakePending(StoryDialogueDelivery delivery, out string node);
    }
}
