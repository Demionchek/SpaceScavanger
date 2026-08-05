using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace Game.UI
{
    public sealed class TutorialPopupUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Button _closeButton;

        private EventBus _eventBus;
        private string _currentMessage;

        [Inject]
        public void Construct(EventBus eventBus)
        {
            _eventBus = eventBus;
            _eventBus.Subscribe<TutorialPopupEvent>(OnPopup);
        }

        private void Awake()
        {
            if (_panel != null)
                _panel.SetActive(false);

            if(_closeButton != null)
                _closeButton.onClick.AddListener(()=> Apply(null, false));
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<TutorialPopupEvent>(OnPopup);
        }

        private void OnPopup(TutorialPopupEvent evt)
        {
            if (evt.Show)
            {
                _currentMessage = evt.Message;
                Apply(evt.Message, true);
                return;
            }

            // Гасим только своё сообщение: зоны могут перекрываться, и выход из
            // предыдущей не должен убирать подсказку следующей.
            if (_currentMessage == evt.Message)
            {
                _currentMessage = null;
                Apply(null, false);
            }
        }

        private void Apply(string message, bool show)
        {
            if (_text != null && show)
            {
                _text.text = message;
            }

            if (_panel != null)
            {
                _panel.SetActive(show);
            }
        }
    }
}
