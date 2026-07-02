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
            _stateMachine.ChangeState<ShipInteriorState>();
        }
    }
}
