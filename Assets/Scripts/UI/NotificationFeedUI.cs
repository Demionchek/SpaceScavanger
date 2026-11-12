using System.Collections;
using System.Collections.Generic;
using Game.Core;
using TMPEffects.Components;
using UnityEngine;
using VContainer;

namespace Game.UI
{
    public sealed class NotificationFeedUI : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private TMPWriter _writer;
        [SerializeField] private float _holdSeconds = 1.5f;

        [Tooltip("Сообщение, пролежавшее в очереди дольше этого, уже не показывается: " +
                 "события из корабля игрок видит там же, а не пачкой при выходе в космос.")]
        [SerializeField] private float _staleSeconds = 6f;

        private EventBus _eventBus;
        private readonly Queue<Entry> _queue = new();
        private bool _showing;

        [Inject]
        public void Construct(EventBus eventBus)
        {
            _eventBus = eventBus;
            _eventBus.Subscribe<NotificationRequestedEvent>(OnNotification);
        }

        private void Awake()
        {
            // Окна верстака и торговца держат паузу, а писатель по умолчанию считает
            // по игровому времени: иначе строки копятся и выходят все разом потом.
            _writer.UseScaledTime = false;
            _writer.OnFinishWriter.AddListener(OnWriteFinished);

            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            _eventBus?.Unsubscribe<NotificationRequestedEvent>(OnNotification);

            if (_writer != null)
            {
                _writer.OnFinishWriter.RemoveListener(OnWriteFinished);
            }
        }

        private void OnNotification(NotificationRequestedEvent evt)
        {
            _queue.Enqueue(new Entry(evt.Message, Time.unscaledTime));

            if (!_showing)
            {
                ShowNext();
            }
        }

        private void ShowNext()
        {
            while (_queue.Count > 0)
            {
                var entry = _queue.Dequeue();
                if (Time.unscaledTime - entry.Queued > _staleSeconds)
                {
                    continue;
                }

                _showing = true;
                if (_root != null)
                {
                    _root.SetActive(true);
                }

                _writer.SetText(entry.Message);
                _writer.RestartWriter();
                return;
            }

            _showing = false;
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        private readonly struct Entry
        {
            public readonly string Message;
            public readonly float Queued;

            public Entry(string message, float queued)
            {
                Message = message;
                Queued = queued;
            }
        }

        private void OnWriteFinished(TMPWriter writer)
        {
            StartCoroutine(HoldThenNext());
        }

        private IEnumerator HoldThenNext()
        {
            yield return new WaitForSecondsRealtime(_holdSeconds);
            ShowNext();
        }
    }
}
