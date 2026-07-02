namespace Game.Core
{
    public interface IGameState
    {
        string ActionMapName { get; }

        void Enter();
        void Exit();
    }
}
