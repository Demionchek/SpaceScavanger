using Game.Core;
using TMPro;
using UnityEngine;
using VContainer;

namespace Game.UI
{
    public sealed class RaceHudUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _raceText;

        private EventBus _eventBus;

        [Inject]
        public void Construct(EventBus eventBus)
        {
            _eventBus = eventBus;
        }

        private void Awake()
        {
            _raceText.text = string.Empty;
        }

        private void OnEnable()
        {
            _eventBus.Subscribe<RaceCountdownEvent>(OnCountdown);
            _eventBus.Subscribe<RaceProgressEvent>(OnProgress);
            _eventBus.Subscribe<RaceFinishedEvent>(OnFinished);
        }

        private void OnDisable()
        {
            _eventBus.Unsubscribe<RaceCountdownEvent>(OnCountdown);
            _eventBus.Unsubscribe<RaceProgressEvent>(OnProgress);
            _eventBus.Unsubscribe<RaceFinishedEvent>(OnFinished);
        }

        private void OnCountdown(RaceCountdownEvent evt)
        {
            _raceText.text = evt.Seconds > 0 ? $"{evt.Seconds}..." : "GO!";
        }

        private void OnProgress(RaceProgressEvent evt)
        {
            _raceText.text = $"Checkpoint {evt.NextCheckpoint}/{evt.CheckpointCount}";
        }

        private void OnFinished(RaceFinishedEvent evt)
        {
            _raceText.text = $"Finished: place {evt.Place}/{evt.Participants}";
        }
    }
}
