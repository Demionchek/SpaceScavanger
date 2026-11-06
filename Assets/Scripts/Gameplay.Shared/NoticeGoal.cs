using Game.Core;

namespace Game.Gameplay.Shared
{
    // Цель-объявление: не выполняется никогда, только держит свою строку в панели
    // заданий. Снимает её тот, кто выдал квест — QuotaService, когда придёт квота.
    public sealed class NoticeGoal : IQuestGoal, IQuestGoalStatus
    {
        private readonly string _status;

        public NoticeGoal(string status)
        {
            _status = status;
        }

        public bool IsComplete => false;

        public float Progress => 0f;

        public string Status => _status;

        public void Bind(EventBus bus)
        {
        }

        public void Unbind(EventBus bus)
        {
        }
    }
}
