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

        private EventBus _eventBus;
        private readonly Queue<string> _queue = new();
        private bool _showing;

        [Inject]
        public void Construct(EventBus eventBus)
        {
            _eventBus = eventBus;
            _eventBus.Subscribe<NotificationRequestedEvent>(OnNotification);
        }

        private void Awake()
        {
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
            _queue.Enqueue(evt.Message);

            if (!_showing)
            {
                ShowNext();
            }
        }

        private void ShowNext()
        {
            if (_queue.Count == 0)
            {
                _showing = false;
                if (_root != null)
                {
                    _root.SetActive(false);
                }

                return;
            }

            _showing = true;
            if (_root != null)
            {
                _root.SetActive(true);
            }

            _writer.SetText(_queue.Dequeue());
            _writer.RestartWriter();
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
