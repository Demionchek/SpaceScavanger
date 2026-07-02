using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Core
{
    public sealed class GameStateMachine
    {
        private readonly EventBus _eventBus;
        private readonly IReadOnlyDictionary<Type, IGameState> _states;

        public IGameState CurrentState { get; private set; }

        public GameStateMachine(EventBus eventBus, IEnumerable<IGameState> states)
        {
            _eventBus = eventBus;
            _states = states.ToDictionary(state => state.GetType(), state => state);
        }

        public void ChangeState<TState>() where TState : class, IGameState
        {
            ChangeState(typeof(TState));
        }

        public void ChangeState(Type stateType)
        {
            if (!_states.TryGetValue(stateType, out var nextState))
            {
                throw new InvalidOperationException(
                    $"State '{stateType.Name}' is not registered with {nameof(GameStateMachine)}.");
            }

            if (ReferenceEquals(CurrentState, nextState))
            {
                return;
            }

            var previousState = CurrentState;
            previousState?.Exit();
            CurrentState = nextState;
            nextState.Enter();

            _eventBus.Publish(new GameStateChangedEvent(previousState, nextState));
        }
    }
}
