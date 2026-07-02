using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace Game.Core
{
    public sealed class InputMapSwitcher : IStartable, IDisposable
    {
        private readonly EventBus _eventBus;
        private readonly InputActionAsset _inputActions;
        private readonly Action<GameStateChangedEvent> _onGameStateChanged;

        public InputMapSwitcher(EventBus eventBus, InputActionAsset inputActions)
        {
            _eventBus = eventBus;
            _inputActions = inputActions;
            _onGameStateChanged = OnGameStateChanged;
        }

        public void Start()
        {
            _eventBus.Subscribe(_onGameStateChanged);
        }

        public void Dispose()
        {
            _eventBus.Unsubscribe(_onGameStateChanged);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            var map = _inputActions.FindActionMap(evt.NewState.ActionMapName, throwIfNotFound: true);

            _inputActions.Disable();
            map.Enable();

            Debug.Log($"[InputMapSwitcher] Active action map: {map.name}");
        }
    }
}
