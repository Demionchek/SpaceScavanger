namespace Game.Core
{
    // Цели с внутренним прогрессом (счётчиком, флагом), который нельзя вывести
    // из других сервисов. Цели, читающие прогресс из ResourceService и т.п., его
    // не реализуют — восстановятся сами.
    public interface ISaveableGoal
    {
        string SaveProgress();
        void LoadProgress(string data);
    }
}
