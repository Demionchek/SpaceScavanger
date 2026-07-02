namespace Game.Core
{
    public readonly struct GameStateChangedEvent
    {
        public readonly IGameState PreviousState;
        public readonly IGameState NewState;

        public GameStateChangedEvent(IGameState previousState, IGameState newState)
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }
}
