using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;

namespace Game.Core
{
    public sealed class PauseService : IPauseService
    {
        private readonly EventBus _eventBus;
        private readonly InputActionAsset _inputActions;
        private readonly GameStateMachine _stateMachine;
        private readonly PauseAudioSettings _audio;
        private int _pauseCount;

        public bool IsPaused => _pauseCount > 0;

        public PauseService(EventBus eventBus, InputActionAsset inputActions, GameStateMachine stateMachine,
            PauseAudioSettings audio)
        {
            _eventBus = eventBus;
            _inputActions = inputActions;
            _stateMachine = stateMachine;
            _audio = audio;

            if (_audio.IsConfigured)
            {
                // Иначе при timeScale = 0 переход между снапшотами не сдвинется.
                _audio.PausedSnapshot.audioMixer.updateMode = AudioMixerUpdateMode.UnscaledTime;

                // Активный снапшот хранится в ассете микшера и переживает перезагрузку
                // сцены: после рестарта из Game Over эффекты остались бы заглушены.
                _audio.GameplaySnapshot.TransitionTo(0f);
            }
        }

        public void RequestPause() => RequestPause(true);

        // Политику звука задаёт САМЫЙ ВНЕШНИЙ запрос: вложенные (диалог внутри
        // катсцены) уже не переглушат — счётчик к тому моменту больше нуля.
        public void RequestPause(bool muteAudio)
        {
            _pauseCount++;
            if (_pauseCount > 1)
            {
                return;
            }

            Time.timeScale = 0f;

            if (muteAudio)
            {
                SetAudioPaused(true);
            }

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
            SetAudioPaused(false);
            _inputActions.FindActionMap(_stateMachine.CurrentState.ActionMapName, throwIfNotFound: true).Enable();
            _eventBus.Publish(new PauseStateChangedEvent(false));
        }

        private void SetAudioPaused(bool paused)
        {
            if (!_audio.IsConfigured)
            {
                AudioListener.pause = paused;
                return;
            }

            var snapshot = paused ? _audio.PausedSnapshot : _audio.GameplaySnapshot;
            snapshot.TransitionTo(_audio.TransitionTime);
        }
    }
}
