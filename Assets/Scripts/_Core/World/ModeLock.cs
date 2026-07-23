namespace Game.Core
{
    // Ref-counted замок на вход в интерьер. Пока держится хоть один лок (бой,
    // гонка), переключение в интерьер запрещено. По образцу PauseService.
    public sealed class ModeLock
    {
        private int _count;

        public bool IsLocked => _count > 0;

        public void AddLock() => _count++;

        public void RemoveLock()
        {
            if (_count > 0)
            {
                _count--;
            }
        }
    }
}
