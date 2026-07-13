namespace Game.Core
{
    public interface IQuestGoal
    {
        bool IsComplete { get; }
        float Progress { get; }
        void Bind(EventBus bus);
        void Unbind(EventBus bus);
    }

    // Цели, требующие что-то отдать при сдаче квеста (например, собранный ресурс).
    public interface IDeliverableGoal
    {
        bool TryDeliver();
    }
}
