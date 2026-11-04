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

    // Цели, которым мало процента: панель квестов покажет эту строку вместо него.
    public interface IQuestGoalStatus
    {
        string Status { get; }
    }
}
