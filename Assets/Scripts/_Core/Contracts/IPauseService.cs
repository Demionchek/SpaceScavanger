namespace Game.Core
{
    public interface IPauseService
    {
        bool IsPaused { get; }
        void RequestPause();

        // muteAudio = false для катсцен: время стоит, но музыка и таймлайн звучат.
        void RequestPause(bool muteAudio);
        void ReleasePause();
    }
}
