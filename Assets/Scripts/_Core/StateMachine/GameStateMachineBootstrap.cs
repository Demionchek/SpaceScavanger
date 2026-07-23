using VContainer.Unity;

namespace Game.Core
{
    public sealed class GameStateMachineBootstrap : IStartable
    {
        private readonly GameStateMachine _stateMachine;

        public GameStateMachineBootstrap(GameStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public void Start()
        {
            // Стартуем в полёте: интерьер — additive-сцена, грузится по запросу,
            // на старте ещё не загружена.
            _stateMachine.ChangeState<SpaceFlightState>();
        }
    }
}
