namespace Game.Core
{
    public interface ISectorService
    {
        int Current { get; }

        void Advance();
    }
}
