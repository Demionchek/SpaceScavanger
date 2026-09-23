namespace Game.Core
{
    public interface IInteractionHintService
    {
        bool IsUsed(string id);
        void MarkUsed(string id);
    }
}
