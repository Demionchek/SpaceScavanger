using Game.Core;
using TMPro;
using UnityEngine;
using VContainer;

namespace Game.UI
{
    public sealed class WarningBannerUI : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private float _messageDuration = 2.5f;

        private EventBus _eventBus;
        private string _persistent;
        private string _message;
        private float _messageUntil;

        [Inject]
        public void Construct(EventBus eventBus)
        {
            _eventBus = eventBus;
            _eventBus.Subscribe<WarningMessageEvent>(OnMessage);
        }

        private void Awake()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<WarningMessageEvent>(OnMessage);
        }

        // Постоянное предупреждение (граница зоны). null — снять.
        public void SetPersistent(string message)
        {
            _persistent = message;
            Apply();
        }

        private void OnMessage(WarningMessageEvent evt)
        {
            _message = evt.Message;
            _messageUntil = Time.unscaledTime + _messageDuration;
            Apply();
        }

        private void Update()
        {
            if (_message != null && Time.unscaledTime >= _messageUntil)
            {
                _message = null;
                Apply();
            }
        }

        private void Apply()
        {
            var shown = _message ?? _persistent;

            if (_text != null && shown != null)
            {
                _text.text = shown;
            }

            if (_root != null)
            {
                _root.SetActive(shown != null);
            }
        }
    }
}
