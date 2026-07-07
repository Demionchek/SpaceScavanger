using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Core
{
    public sealed class PauseService : IPauseService
    {
        private readonly EventBus _eventBus;
        private readonly InputActionAsset _inputActions;
        private readonly GameStateMachine _stateMachine;
        private int _pauseCount;

        public bool IsPaused => _pauseCount > 0;

        public PauseService(EventBus eventBus, InputActionAsset inputActions, GameStateMachine stateMachine)
        {
            _eventBus = eventBus;
            _inputActions = inputActions;
            _stateMachine = stateMachine;
        }

        public void RequestPause()
        {
            _pauseCount++;
            if (_pauseCount > 1)
            {
                return;
            }

            Time.timeScale = 0f;
            AudioListener.pause = true;
            _inputActions.Disable();
            _eventBus.Publish(new PauseStateChangedEvent(true));
        }

        public void ReleasePause()
        {
            if (_pauseCount == 0)
            {
                return;
            }

            _pauseCount--;
            if (_pauseCount > 0)
            {
                return;
            }

            Time.timeScale = 1f;
            AudioListener.pause = false;
            _inputActions.FindActionMap(_stateMachine.CurrentState.ActionMapName, throwIfNotFound: true).Enable();
            _eventBus.Publish(new PauseStateChangedEvent(false));
        }
    }
}
