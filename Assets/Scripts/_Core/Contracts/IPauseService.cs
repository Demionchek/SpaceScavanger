namespace Game.Core
{
    public interface IPauseService
    {
        bool IsPaused { get; }
        void RequestPause();
        void ReleasePause();
    }
}
