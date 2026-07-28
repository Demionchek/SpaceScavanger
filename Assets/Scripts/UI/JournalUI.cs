using System.Text;
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Game.UI
{
    public sealed class JournalUI : MonoBehaviour
    {
        [SerializeField] private GameObject _panel;
        [SerializeField] private TMP_Text _text;

        private IJournalService _journal;
        private EventBus _eventBus;
        private IPauseService _pauseService;
        private readonly StringBuilder _builder = new();
        private bool _open;

        [Inject]
        public void Construct(IJournalService journal, EventBus eventBus, IPauseService pauseService)
        {
            _journal = journal;
            _eventBus = eventBus;
            _pauseService = pauseService;
            _eventBus.Subscribe<JournalChangedEvent>(OnJournalChanged);
        }

        private void Awake()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<JournalChangedEvent>(OnJournalChanged);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.jKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        private void Toggle()
        {
            _open = !_open;
            if (_panel != null)
            {
                _panel.SetActive(_open);
            }

            if (_open)
            {
                Rebuild();
                _pauseService.RequestPause();
            }
            else
            {
                _pauseService.ReleasePause();
            }
        }

        private void OnJournalChanged(JournalChangedEvent _)
        {
            if (_open)
            {
                Rebuild();
            }
        }

        private void Rebuild()
        {
            if (_text == null)
            {
                return;
            }

            _builder.Clear();
            var entries = _journal.Entries;
            for (var i = entries.Count - 1; i >= 0; i--)
            {
                _builder.Append("• ").AppendLine(entries[i].Message);
            }

            _text.text = _builder.ToString();
        }
    }
}
