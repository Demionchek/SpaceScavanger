using System;
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

namespace Game.UI
{
    public sealed class GameOverUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _reasonLabel;
        [SerializeField] private Button _restartButton;

        private EventBus _eventBus;
        private IPauseService _pauseService;

        [Inject]
        public void Construct(EventBus eventBus, IPauseService pauseService)
        {
            _eventBus = eventBus;
            _pauseService = pauseService;
            _eventBus.Subscribe<GameOverEvent>(OnGameOver);
        }

        private void Awake()
        {
            if(_restartButton != null)
                _restartButton.onClick.AddListener(Restart);
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<GameOverEvent>(OnGameOver);
        }

        private void OnGameOver(GameOverEvent evt)
        {
            if (_reasonLabel != null)
            {
                _reasonLabel.text = evt.Reason;
            }

            if (_panel != null)
            {
                _panel.SetActive(true);
            }

            _pauseService.RequestPause();
        }

        public void Restart()
        {
            // Глобальные статики не сбрасываются перезагрузкой сцены, а PauseService
            // (с его ReleasePause) при этом уничтожается — сбрасываем вручную.
            Time.timeScale = 1f;
            AudioListener.pause = false;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
